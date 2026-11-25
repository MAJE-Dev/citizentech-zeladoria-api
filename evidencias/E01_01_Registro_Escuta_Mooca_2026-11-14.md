# E01 — Registro de observação e escuta do território (R1)

| Campo | Registro |
|---|---|
| Data e hora | 14/11/2026, por volta das 14h30 |
| Local / contexto | Via pública na **Mooca**, São Paulo/SP (região da Subprefeitura Mooca). A conversa aconteceu na rua onde o problema é visível: esquina com buraco e quarteirão com postes apagados no trajeto até o ponto de ônibus. |
| Quem da equipe ouviu | Vitor Rivas Cardoso (VitorCardoso_RM556404) |
| Pessoa ouvida (perfil anônimo) | Morador/trabalhador adulto que passa pela rua todos os dias para ir trabalhar. **Não registramos nome, contato, áudio nem imagem da pessoa.** |
| Forma | Conversa breve e presencial, registrada por escrito pelo integrante logo após o contato (transcrição abaixo). |

## Transcrição do registro (anônima)

> **Integrante:** Oi, tudo bem? Posso fazer umas perguntas rápidas sobre a situação aqui da rua?
> **Morador:** Opa, tudo certo! Pode mandar.
> **Integrante:** Você mora ou passa por aqui todo dia? Como tá a situação do asfalto, dos buracos?
> **Morador:** Passo todo dia pra ir trabalhar. Olha, tá bem complicado. Ali na esquina mesmo tem um buraco fundo que já faz meses que ninguém arruma. Quando chove, enche d'água e quem não conhece cai direto.
> **Integrante:** E a iluminação à noite, os postes estão funcionando?
> **Morador:** Metade tá apagada. Depois das sete da noite fica um breu só nesse quarteirão, dá até medo de voltar a pé do ponto de ônibus.
> **Integrante:** Vocês já chegaram a abrir reclamação na prefeitura ou pedir manutenção?
> **Morador:** Já abrimos protocolo umas duas vezes pelo aplicativo, mas até agora nada. Só dizem que está em análise.
> **Integrante:** Valeu pela atenção! Tomara que resolvam logo.
> **Morador:** Imagina, eu que agradeço!

## Síntese do que foi ouvido

1. **Pavimentação:** um buraco fundo na esquina está sem reparo há meses. Com chuva ele fica cheio d'água, fica invisível e causa quedas.
2. **Iluminação:** cerca de metade dos postes do quarteirão fica apagada depois das 19h, o que traz insegurança no trajeto a pé a partir do ponto de ônibus.
3. **Atendimento público:** o morador já abriu protocolo duas vezes pelo aplicativo oficial e não teve retorno além de "em análise". Ele não consegue ver o andamento nem o quanto falta.

## Como o relato orientou a solução (ligação com o recorte)

| O que foi ouvido | Decisão na Zeladoria API Urbana |
|---|---|
| Buraco e postes são os problemas do dia a dia | As categorias **Pavimentação** e **Iluminação pública** entram na carga. A ocorrência 1 (fictícia) reproduz o cenário relatado. |
| "Quem não conhece cai direto": o problema precisa ser visto | A **foto do problema** fica vinculada à ocorrência e guardada em bytes no banco (IMAGEM_OCORRENCIA). Ela serve como prova para a triagem e para comparar antes e depois. |
| "Só dizem que está em análise" | Cada ocorrência mostra **situação** (ABERTA, EM_ATENDIMENTO ou CONCLUIDA), **quantidade pendente** (m², pontos de luz…) e o **histórico de atendimentos**. A baixa do pendente e o registro do atendimento são atômicos (transação), então o cidadão nunca vê um estado incoerente. |
| Demora de meses | O indicador proposto mede o **percentual de ocorrências concluídas em até 30 dias** (ver README e relatório). |

**Limitações declaradas:** foi uma única conversa, sem valor estatístico, e não confirmamos de forma independente os dois protocolos citados. Não houve pesquisa em outra disciplina usada nesta entrega. Os dados de domínio da carga (endereços e quantidades) são **fictícios**. Só a escuta é um registro real.
