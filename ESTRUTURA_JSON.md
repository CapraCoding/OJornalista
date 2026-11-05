# Estrutura dos JSONs — Jornalista

Os dois arquivos ficam em `Assets/Resources/` e devem ser salvos em **UTF-8**.

| Arquivo | Para quê |
|---|---|
| `noticias.json` | As laudas (uma por dia) e as regras gerais |
| `sinonimos.json` | Grupos de termos que podem substituir um ao outro nas frases válidas |

---

## 1. `noticias.json`

### Nível raiz

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `manchetesPorDia` | número | sim | Quantas laudas o jogador faz por dia. Hoje: **1** |
| `embaralharOpcoes` | true/false | não | Embaralha a ordem dos blocos na tela |
| `naoRepetirFrases` | true/false | não | Recusa frase que já foi ao ar (obriga trocar o termo) |
| `errosParaIrPraCasa` | número | não (padrão 3) | Erros de manchete no mesmo dia até o editor mandar o jornalista pra casa (dia perdido = 1 advertência) |
| `advertenciasParaDemissao` | número | não (padrão 3) | Advertências em dias seguidos até a demissão (game over, volta ao dia 1). Publicar certo zera a sequência |
| `chamadasGenericas` | lista de textos | não | Chamadas sensacionalistas usadas quando a notícia não tem as suas |
| `dias` | lista de **Dia** | sim | Um item por dia, em ordem. Depois do último, o ciclo recomeça |

### Dia

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `nome` | texto | não | Só para você se organizar |
| `noticias` | lista de **Notícia** | sim | Com `manchetesPorDia: 1`, coloque 1 notícia por dia |

### Notícia (a lauda)

| Campo | Tipo | Obrigatório | Onde aparece | Exemplo |
|---|---|---|---|---|
| `titulo` | texto | sim | Topo da lauda / nome do arquivo na lista | `ACIDENTE EM CASCAVEL` |
| `texto` | texto | sim | Meio da lauda; lido na TV. `\n` separa parágrafos | `Acidente envolvendo...\nCom a força...` |
| `local` | texto | não | Tarja vermelha do GC (o "FRASE:" da lauda) | `BRASMADEIRA, EM CASCAVEL` |
| `campos` | lista de **Campo** | sim | Blocos que o jogador escolhe para montar a frase | ver abaixo |
| `frasesValidas` | lista de textos | sim | Frases aceitas como corretas | `Mulher fica ferida em acidente entre carros` |
| `chamadasSensacionalistas` | lista de textos | não | O que a emissora coloca por cima (outdoor e TV) | `MAIS UMA TRAGÉDIA!` |

### Campo (um bloco da frase)

| Campo | Tipo | Obrigatório | Descrição | Exemplo |
|---|---|---|---|---|
| `rotulo` | texto | sim | Nome do bloco na tela | `Quem` |
| `opcoes` | lista de textos | sim | Escolhas do jogador (inclua as erradas também) | `["Mulher", "Passageira", "Criança"]` |

### Como a frase é validada

1. A frase do jogador = opções escolhidas, **juntadas com espaço, na ordem dos `campos`**.
   `Quem` + `O quê` + `Como` → `Passageira` + `fica ferida` + `em acidente entre carros`
2. A comparação ignora maiúsculas/minúsculas, espaços duplos e pontuação no final.
3. Uma frase é aceita se bater com **qualquer** item de `frasesValidas`, considerando:
   - **Sinônimos do dicionário** (`sinonimos.json`): escreva só `Mulher fica ferida...` e o jogo aceita `Passageira`, `Motorista` etc.
   - **Alternativas locais** com chaves, para valer só nesta notícia: `{Mulher|Jovem} fica ferida em acidente entre carros`.
4. Se `naoRepetirFrases` for true e a frase exata já foi ao ar no ciclo, o editor recusa: o jogador precisa trocar um termo.

> Toda frase válida tem que ser montável com as `opcoes` dos `campos`. Se a frase usa um sinônimo, pelo menos uma das opções tem que ser desse grupo.

### Modelo vazio (copie e preencha)

```json
{
  "manchetesPorDia": 1,
  "embaralharOpcoes": true,
  "naoRepetirFrases": true,
  "errosParaIrPraCasa": 3,
  "advertenciasParaDemissao": 3,
  "chamadasGenericas": ["PLANTÃO", "CIDADE EM ALERTA"],
  "dias": [
    {
      "nome": "Dia 1",
      "noticias": [
        {
          "titulo": "",
          "texto": "Parágrafo 1...\nParágrafo 2...",
          "local": "",
          "campos": [
            { "rotulo": "Quem",  "opcoes": ["", "", ""] },
            { "rotulo": "O quê", "opcoes": ["", "", ""] },
            { "rotulo": "Como",  "opcoes": ["", "", ""] }
          ],
          "frasesValidas": [""],
          "chamadasSensacionalistas": ["", ""]
        }
      ]
    }
  ]
}
```

---

## 2. `sinonimos.json`

### Estrutura

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `grupos` | lista de **Grupo** | sim | Cada grupo reúne termos intercambiáveis |

### Grupo

| Campo | Tipo | Obrigatório | Descrição | Exemplo |
|---|---|---|---|---|
| `nome` | texto | não | Só para organização | `motociclista` |
| `termos` | lista de textos | sim (mín. 2) | Palavras **ou expressões** equivalentes | `["Motociclista", "Piloto", "Condutor", "Homem"]` |

### Regras

- Vale para palavras soltas e expressões inteiras (`"chuva forte"`, `"acidente entre carros"`).
- A troca só acontece em **palavra inteira**: `homem` não troca dentro de `homenagem`.
- Maiúsculas/minúsculas não importam; **acentos importam** (`vitima` ≠ `vítima`).
- **Separe por gênero e número**, senão a frase fica errada:
  `mulher/passageira` combina com `fica ferida`; `homem/piloto` combina com `fica ferido`.
- Um termo pode estar em mais de um grupo: ele aceita os termos de todos os grupos em que aparece.
- Se dois termos se sobrepõem (`chuva` e `chuva forte`), o mais comprido tem prioridade.

### Exemplo

```json
{
  "grupos": [
    { "nome": "vitima feminina", "termos": ["Mulher", "Passageira", "Motorista"] },
    { "nome": "motociclista",    "termos": ["Motociclista", "Piloto", "Condutor", "Homem"] },
    { "nome": "chuva",           "termos": ["Chuva forte", "Temporal", "Tempestade"] },
    { "nome": "batida",          "termos": ["acidente entre carros", "batida entre carros", "colisão entre carros"] }
  ]
}
```

---

## Exemplo completo de uma lauda

A lauda de papel vira isto:

| Na lauda | No JSON |
|---|---|
| ACIDENTE EM CASCAVEL | `titulo` |
| ACIDENTE ENVOLVENDO DOIS CARROS... (parágrafos) | `texto` (um `\n` entre parágrafos) |
| FRASE: BRASMADEIRA, EM CASCAVEL /// | `local` (o `///` é colocado pelo jogo) |
| MULHER FICA FERIDA EM ACIDENTE ENTRE CARROS | `frasesValidas` + os blocos em `campos` |
