using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// O loop de mineração no mapa (M2): o anão escolhe o alvo (D-27), anda até ele, bate a cada intervalo, quebra,
    /// soma lascas e escolhe o próximo; cristais nascem a cada intervalo até o máximo. Desde o M3, dano, intervalo,
    /// caminhada e lascas vêm dos atributos (<see cref="MinerStats"/>), que as compras (<see cref="Purchase"/>) melhoram.
    ///
    /// O tempo é do código (Arquitetura Técnica, D-T2): <see cref="Tick"/> avança de evento em evento (chegada, golpe,
    /// nascimento), então um <c>Tick(600)</c> dá o mesmo resultado que 36.000 <c>Tick(1/60)</c>. É o que permite o
    /// ×20 do debug, o simulador e, no futuro, o progresso offline.
    /// </summary>
    public sealed class MiningSimulation
    {
        /// <summary>Folga numérica para "chegou" e "deu a hora": evita ficar preso a 1e-15 de um evento.</summary>
        const double Epsilon = 1e-9;

        /// <summary>Trava contra laço infinito por bug: 1 h de jogo tem poucos milhares de eventos.</summary>
        const int MaxStepsPerTick = 10_000_000;

        readonly MiningRules _rules;
        readonly CrystalField _field;
        readonly Miner _miner;
        readonly GameState _state;
        readonly EconomyRules _economy;

        MinerStats _stats;
        double _spawnTimer;

        /// <param name="economy">Melhorias à venda. Sem ela, nenhuma linha vende e os atributos são os valores base (o jogo do M2).</param>
        public MiningSimulation(MiningRules rules, IRandom random, GameState state = null, EconomyRules economy = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _state = state ?? new GameState();
            // Sem economia, o mínimo do intervalo é o próprio intervalo base: os atributos ficam iguais aos valores base.
            _economy = economy ?? EconomyRules.None(minHitInterval: rules.HitInterval);
            _stats = MinerStats.Compute(_rules, _economy, _state);
            _miner = new Miner(rules.MinerStart);
            _field = new CrystalField(rules, random);

            if (rules.InitialCrystals > 0)
                _field.SpawnBatch(rules.InitialCrystals, _miner.Position);
        }

        /// <summary>Um cristal nasceu. Os do início já existem ao construir: quem desenha lê <see cref="Crystals"/> primeiro.</summary>
        public event Action<Crystal> CrystalSpawned;
        /// <summary>O alvo do anão mudou (<c>null</c> = ficou sem alvo).</summary>
        public event Action<Crystal> TargetChanged;
        /// <summary>Golpe no cristal, com o dano dado.</summary>
        public event Action<Crystal, double> HitLanded;
        /// <summary>O cristal quebrou e saiu do mapa; a recompensa já foi somada.</summary>
        public event Action<Crystal> CrystalBroken;
        /// <summary>O total de lascas mudou.</summary>
        public event Action<double> ShardsChanged;
        /// <summary>Uma melhoria foi comprada, com o nível novo.</summary>
        public event Action<UpgradeId, int> UpgradePurchased;
        /// <summary>Os atributos do anão mudaram (DANO e VELOCIDADE do HUD).</summary>
        public event Action<MinerStats> StatsChanged;

        public MiningRules Rules => _rules;
        public EconomyRules Economy => _economy;

        /// <summary>Atributos atuais do anão: valores base + melhorias.</summary>
        public MinerStats Stats => _stats;
        public GameState State => _state;
        public Miner Miner => _miner;

        /// <summary>Cristais inteiros no mapa, na ordem de nascimento.</summary>
        public IReadOnlyList<Crystal> Crystals => _field.Crystals;

        /// <summary>Segundos de jogo simulados.</summary>
        public double Elapsed { get; private set; }

        /// <summary>Avança <paramref name="dt"/> segundos de jogo.</summary>
        public void Tick(double dt)
        {
            if (double.IsNaN(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), "O tempo não anda para trás.");

            double remaining = dt;
            int steps = 0;
            while (true)
            {
                ProcessDueEvents();
                if (remaining <= 0) break;

                double step = Math.Min(remaining, TimeToNextEvent());
                Advance(step);
                remaining -= step;

                if (++steps > MaxStepsPerTick)
                    throw new InvalidOperationException("MiningSimulation.Tick não avança: o laço de eventos travou.");
            }
        }

        /// <summary>Cria um cristal na posição dada, com lote próprio, ignorando espaço e máximo (testes e debug).</summary>
        public Crystal SpawnCrystalAt(Vec2 position)
        {
            var crystal = _field.SpawnAt(position);
            CrystalSpawned?.Invoke(crystal);
            return crystal;
        }

        /// <summary>
        /// Atalho do debug: um cristal agora, num lugar livre e com lote próprio, sem esperar o relógio e ignorando o
        /// máximo. <c>null</c> se não houver lugar livre.
        /// </summary>
        public Crystal SpawnCrystalNow()
        {
            var crystal = _field.TrySpawn(_miner.Position, ignoreMax: true);
            if (crystal != null) CrystalSpawned?.Invoke(crystal);
            return crystal;
        }

        /// <summary>
        /// Atalho do debug: quebra o alvo atual na hora, pelo mesmo caminho de uma quebra normal (paga a recompensa, conta
        /// a quebra, mas não conta golpe). O próximo alvo é escolhido no próximo <see cref="Tick"/>. <c>false</c> sem alvo.
        /// </summary>
        public bool BreakTarget()
        {
            var target = _miner.Target;
            if (target == null) return false;

            target.ApplyDamage(target.Health);
            Break(target);
            return true;
        }

        /// <summary>Atalho do debug: soma lascas sem quebrar cristal.</summary>
        public void AddShards(double amount)
        {
            if (amount < 0 || double.IsNaN(amount)) throw new ArgumentOutOfRangeException(nameof(amount));

            _state.AddShards(amount);
            ShardsChanged?.Invoke(_state.Shards);
        }

        /// <summary>
        /// Compra a próxima melhoria da linha (REQ-ECO-08). Se der certo, os atributos novos valem a partir de agora:
        /// golpe com intervalo menor sai no próximo <see cref="Tick"/> se o tempo acumulado já passou dele; caminhada mais
        /// rápida vale do ponto onde o anão está. Sem compra, nada muda e nenhum evento sai.
        /// </summary>
        public PurchaseResult Purchase(UpgradeId id)
        {
            var result = Purchases.TryPurchase(_state, _economy, id);
            if (result != PurchaseResult.Purchased) return result;

            _stats = MinerStats.Compute(_rules, _economy, _state);
            UpgradePurchased?.Invoke(id, _state.LevelOf(id));
            StatsChanged?.Invoke(_stats);
            ShardsChanged?.Invoke(_state.Shards);
            return result;
        }

        /// <summary>Resolve tudo o que vence agora, na ordem: nascimento, escolha de alvo, golpe.</summary>
        void ProcessDueEvents()
        {
            bool changed;
            do
            {
                changed = false;

                if (!_field.IsFull && _spawnTimer >= _rules.SpawnInterval - Epsilon)
                {
                    _spawnTimer = Math.Max(0, _spawnTimer - _rules.SpawnInterval);
                    var crystal = _field.TrySpawn(_miner.Position); // sem lugar livre: o nascimento é pulado
                    if (crystal != null) CrystalSpawned?.Invoke(crystal);
                    changed = true;
                }

                if (_miner.State == MinerState.Idle)
                {
                    var target = TargetSelector.Select(_field.Crystals, _miner.Position);
                    if (target != null)
                    {
                        StartTarget(target);
                        changed = true;
                    }
                }

                if (_miner.State == MinerState.Mining && _miner.HitTimer >= _stats.HitInterval - Epsilon)
                {
                    _miner.HitTimer = Math.Max(0, _miner.HitTimer - _stats.HitInterval);
                    Hit();
                    changed = true;
                }
            } while (changed);
        }

        double TimeToNextEvent()
        {
            double next = double.PositiveInfinity;

            if (!_field.IsFull)
                next = Math.Max(0, _rules.SpawnInterval - _spawnTimer);

            switch (_miner.State)
            {
                case MinerState.Walking:
                    double toGo = Vec2.Distance(_miner.Position, _miner.Target.Position) - _rules.MiningRange;
                    next = Math.Min(next, Math.Max(0, toGo / _stats.WalkSpeed));
                    break;
                case MinerState.Mining:
                    next = Math.Min(next, Math.Max(0, _stats.HitInterval - _miner.HitTimer));
                    break;
            }

            return next;
        }

        void Advance(double step)
        {
            if (step <= 0) return;

            Elapsed += step;

            // Mapa cheio: o relógio de nascimento espera no limite e nasce assim que alguém quebra.
            _spawnTimer = _field.IsFull ? Math.Min(_spawnTimer + step, _rules.SpawnInterval) : _spawnTimer + step;

            switch (_miner.State)
            {
                case MinerState.Walking:
                    Walk(step);
                    break;
                case MinerState.Mining:
                    _miner.HitTimer += step;
                    break;
            }
        }

        void Walk(double step)
        {
            var toTarget = _miner.Target.Position - _miner.Position;
            double distance = toTarget.Length;
            double toGo = distance - _rules.MiningRange;
            double travel = _stats.WalkSpeed * step;

            if (travel >= toGo - Epsilon)
            {
                // Chegou: para exatamente no alcance, do lado de onde veio (linha reta, D-31).
                _miner.Position = _miner.Target.Position - toTarget * (_rules.MiningRange / distance);
                _miner.State = MinerState.Mining;
                _miner.HitTimer = 0;
            }
            else
            {
                _miner.Position += toTarget * (travel / distance);
            }
        }

        void StartTarget(Crystal target)
        {
            _miner.Target = target;
            _miner.HitTimer = 0;
            bool inRange = Vec2.Distance(_miner.Position, target.Position) <= _rules.MiningRange + Epsilon;
            _miner.State = inRange ? MinerState.Mining : MinerState.Walking;
            TargetChanged?.Invoke(target);
        }

        void Hit()
        {
            var target = _miner.Target;
            double damage = _stats.Damage;

            _state.RegisterHit();
            bool broke = target.ApplyDamage(damage);
            HitLanded?.Invoke(target, damage);

            if (broke) Break(target);
        }

        void Break(Crystal target)
        {
            _field.Remove(target);
            _state.RegisterBreak(target.Reward * _stats.RewardMultiplier);
            _miner.Target = null;
            _miner.State = MinerState.Idle;

            CrystalBroken?.Invoke(target);
            ShardsChanged?.Invoke(_state.Shards);
            TargetChanged?.Invoke(null);
        }
    }
}
