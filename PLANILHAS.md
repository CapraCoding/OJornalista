# Planilhas do Jornalista

As notícias saem de duas planilhas em `Assets/Planilhas/`:

| Arquivo | Conteúdo |
|---|---|
| `modelos.xlsx` | Os modelos de resumo (1 linha = 1 modelo de notícia) |
| `vocabulario.xlsx` | Abas **Sinônimos**, **Termos e estruturas** e **Valores** |

Depois de editar no Excel: salve, volte ao Unity e clique em **Jornalista → Importar planilhas** (menu no topo).
Isso gera `Assets/Resources/noticias.json` e `sinonimos.json`. As configurações gerais (erros, advertências etc.)
continuam no `noticias.json` e não são apagadas pela importação.

## modelos.xlsx

| Coluna | O que é | Separador |
|---|---|---|
| `titulo` | Nome do arquivo na lista da redação | - |
| `texto` | Corpo da lauda. Cada frase vira uma linha com "..." | - |
| `local` | Tarja do GC (o jogo coloca em maiúsculas e acrescenta `//`) | - |
| `frasesValidas` | Frases corretas. Use `|` para separar os blocos que o jogador monta | `;` entre frases |
| `chamadasSensacionalistas` | O que a emissora coloca por cima no outdoor e na TV | `;` |

Exemplo de frase: `Motociclista | fica ferido | em batida com carro` vira 3 blocos.
Sem `|`, o jogo tenta dividir sozinho (quem | verbo | resto).

### Placeholders `[...]`

Qualquer `[nome]` é sorteado a cada dia a partir da aba **Valores**:
- O mesmo placeholder vale o mesmo valor em toda a notícia (texto, local, frases e chamadas).
- `[Bairro]` com maiúscula sai com a primeira letra maiúscula.
- `[Siate/Samu]` sem linha na aba Valores sorteia uma das opções da barra.
- Trechos como `, se informado`, `, se houver` e `quando necessário` são ignorados.
- Placeholder sem valor aparece como está e gera um aviso no Console.

Os mesmos modelos voltam em outros dias com bairro, rua, números e equipes diferentes.

## vocabulario.xlsx

| Aba | Colunas | Uso no jogo |
|---|---|---|
| **Sinônimos** | Chave, Palavras (`,`) | Termos aceitos um no lugar do outro e opções extras certas nos blocos |
| **Termos e estruturas** | Chave, Palavras (`;`) | Listas reaproveitáveis com `@Chave` e o **Manual de redação** do jogo |
| **Valores** | Chave, Valores (`;`) | O que pode ser sorteado em cada `[placeholder]` |

- Na aba Valores, `@Dinâmica` sorteia um item da linha "Dinâmica" da aba Termos (ou de outra linha de Valores).
- O Manual de redação mostra as linhas: Padrão de escrita, Cuidados, Ordem do resumo, Pontuação e leitura e Veículos.

## Como os blocos são montados

Para cada bloco da frase sorteada o jogo oferece:
1. o bloco certo;
2. uma versão com sinônimo (também certa, útil quando a frase já foi ao ar);
3. o mesmo bloco com outro valor sorteado (ex.: "deixa 3 feridos" no lugar de "deixa 2 feridos");
4. blocos de outros modelos na mesma posição, até completar `opcoesPorCampo` (padrão 4).
