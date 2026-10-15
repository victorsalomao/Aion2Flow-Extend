# Roteiro de capturas (T5.1)

Objetivo: gravar sessões de pacotes do jogo, só leitura. Nada de automação, nada enviado ao jogo.

## 1. Como ativar a gravação

- A gravação de pacotes só fica ativa em build **Debug** (`RawPacketDump.IsEnabled` é `true` apenas com `#if DEBUG`). Em Release não grava nada.
- Rode o Aion2Flow em Debug e inicie a captura normalmente pela interface.
- Cada início de captura chama `RawPacketDump.RotateLogs()`, que cria uma pasta nova de sessão. Então **cada sessão = um início de captura**.
- Pasta de saída: `<pasta de trabalho do app>\logs\dumps\<yyyyMMddHHmmss>\`
  - `raw.log`: pacotes brutos
  - `stream.log`: stream reconstruído (o replay usa este formato de linha: direção, timestamp, payload)
- Ao terminar cada sessão, copie o `stream.log` para `tests/captures/` com o nome indicado abaixo.

## 2. Sessões a gravar

Para cada item: inicie a captura → espere ~30 s parado → faça a ação → espere ~30 s → pare a captura. Anote o horário exato da ação no seu bloco de notas.

| # | Arquivo em `tests/captures/` | Ação |
|---|---|---|
| 01 | `01-baseline-parado.log` | Só ficar parado na cidade, sem ação |
| 02 | `02-resina-antes.log` | Antes de gastar resina |
| 03 | `03-resina-depois.log` | Depois de gastar resina |
| 04 | `04-gold-antes.log` | Antes de ganhar gold (ex.: vender/coletar algo) |
| 05 | `05-gold-depois.log` | Depois de ganhar gold |
| 06 | `06-abismo-pontos-antes.log` | Antes de ganhar pontos de abismo |
| 07 | `07-abismo-pontos-depois.log` | Depois de ganhar pontos de abismo |
| 08 | `08-troca-personagem-antes.log` | Na tela de personagem atual, antes de trocar |
| 09 | `09-troca-personagem-depois.log` | Depois de trocar de personagem |

Sugestão: grave "antes" e "depois" como duas capturas separadas, para não misturar as duas.

## 3. Checklist final

- [ ] Build Debug em uso
- [ ] Uma pasta em `logs\dumps\` por sessão (9 no total)
- [ ] `stream.log` copiado para `tests/captures/` com o nome da tabela
- [ ] Horário de cada ação anotado (bloco de notas, mandar junto)
- [ ] Nenhum arquivo commitado no git (`tests/captures/` pode conter dados sensíveis)
