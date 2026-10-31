"""Prévia da beira do caminho (M1b E11): folha 4x4 de quadros de 64 px com pedras, cascalho e enfeites. As formas vêm
dos props do Pixel Fantasy Caves, recoloridas com a paleta cinza da pedra que o jogo já usa (rock_t1) e reduzidas para a
densidade do corredor (64 px por metro). Fica em Generated/ (fora do git, como as outras texturas derivadas do pack)."""
from PIL import Image

PACK = r"E:/Fabrica/Unity/miners-idle/Assets/_Vendor/AssetStore/PixelFantasyCaves/"
ROCK_T1 = r"E:/Fabrica/Unity/miners-idle/Assets/_Vendor/AssetStore/Misc/rock_t1.png"
FRAME, COLUMNS = 64, 4

props1 = Image.open(PACK + "props1.png").convert("RGBA")
props2 = Image.open(PACK + "props2.png").convert("RGBA")
rock_t1 = Image.open(ROCK_T1).convert("RGBA")


def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


# Paleta da pedra do jogo, do escuro ao claro.
PALETTE = sorted({c[:3] for c in rock_t1.getdata() if c[3] > 0}, key=lum)


def recolor(piece):
    """Troca cada cor pela da paleta da rock_t1 na mesma posição de brilho (o claro da forma vira o claro da pedra)."""
    px = piece.load()
    # Por posição (equalizado): cada pedra usa a rampa toda, senão o pack escuro cai inteiro no fundo da paleta.
    values = sorted(lum(px[x, y]) for y in range(piece.height) for x in range(piece.width) if px[x, y][3] > 0)
    for y in range(piece.height):
        for x in range(piece.width):
            c = px[x, y]
            if c[3] == 0:
                continue
            rank = values.index(lum(c)) / max(1, len(values) - 1)
            t = 0.12 + rank * 0.88  # o preto puro fica só no contorno mais escuro
            px[x, y] = PALETTE[min(len(PALETTE) - 1, int(t * len(PALETTE)))] + (255,)
    return piece


def shrink(sheet, box, factor):
    piece = sheet.crop(box)
    piece = piece.crop(piece.getbbox())
    w, h = max(1, round(piece.width / factor)), max(1, round(piece.height / factor))
    pre = Image.new("RGBA", piece.size)
    src, dst = piece.load(), pre.load()
    for y in range(piece.height):
        for x in range(piece.width):
            r, g, b, a = src[x, y]
            dst[x, y] = (r * a // 255, g * a // 255, b * a // 255, a)
    small = pre.resize((w, h), Image.BOX)
    out = Image.new("RGBA", (w, h))
    s, o = small.load(), out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = s[x, y]
            o[x, y] = (min(255, r * 255 // a), min(255, g * 255 // a), min(255, b * 255 // a), 255) if a >= 128 else (0, 0, 0, 0)
    return out


SHAPES = [(176, 99, 336, 158), (352, 78, 451, 158), (464, 96, 637, 158), (912, 72, 1030, 158), (1040, 86, 1149, 158)]
SMALL = [(656, 105, 704, 158), (1168, 112, 1232, 158), (16, 400, 80, 450), (544, 393, 592, 450)]

pieces = [rock_t1.crop(rock_t1.getbbox())]                                   # 0: a pedra de hoje
pieces += [recolor(shrink(props1, b, 4.5)) for b in SHAPES]                  # 1–5: formas novas, tamanho da de hoje
pieces += [recolor(shrink(props1, b, 7)) for b in SHAPES[:3]]                # 6–8: as mesmas, menores
pieces += [recolor(shrink(props1, b, 6)) for b in SMALL]                     # 9–12: cascalho
pieces += [shrink(props2, (690, 110, 784, 176), 5), shrink(props2, (855, 101, 988, 176), 6),
           shrink(props2, (23, 864, 376, 1008), 8)]                          # 13–15: mato e pedra com musgo

sheet = Image.new("RGBA", (FRAME * COLUMNS, FRAME * COLUMNS))
for index, piece in enumerate(pieces):
    assert piece.width <= FRAME and piece.height <= FRAME, (index, piece.size)
    column, row = index % COLUMNS, index // COLUMNS
    sheet.alpha_composite(piece, (column * FRAME + (FRAME - piece.width) // 2, row * FRAME + FRAME - piece.height))
    print(index, piece.size)

sheet.save(PACK + "Generated/edge_sheet.png")
sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(
    r"E:/Fabrica/Unity/miners-idle/tools/ci/out/beira-folha.png")
print("ok")
