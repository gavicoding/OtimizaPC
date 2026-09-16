# ⚡ OtimizaPC

Um programa desktop pra Windows 100% focado em **deixar o PC no melhor desempenho**: limpa o que é lixo de verdade, mostra o que está ocupando espaço, ajusta configurações de performance e remove resíduos de programas desinstalados — tudo isso sem nunca apagar nada sem você confirmar antes.

Construído com **.NET 8 + WPF**, interface em tema escuro, roda nativo no Windows (sem instalar nada extra — existe uma versão publicada como executável único).

---

## O que ele faz

### 📊 Painel
Visão geral da saúde da máquina em tempo real: uso de CPU, memória RAM, espaço em cada unidade de disco e status de saúde (S.M.A.R.T.) quando rodando como administrador. Tem um botão pra criar um **ponto de restauração do Windows** antes de aplicar qualquer ajuste — se algo não sair como esperado, dá pra desfazer.

### 🧹 Limpeza
Escaneia e mostra o tamanho de cada categoria antes de apagar qualquer coisa:
- Temporários do usuário e do Windows, cache de miniaturas, relatórios de erro
- Cache de navegação do Chrome, Edge e Firefox (sem tocar em senhas, histórico ou favoritos)
- Prefetch, cache do Windows Update, logs do sistema, restos de upgrades antigos do Windows
- Lixeira de todas as unidades
- **Arquivos temporários de outras contas de usuário** da mesma máquina (detecta automaticamente outros perfis do Windows)

Nada é excluído sem você revisar a lista e confirmar — o app mostra exatamente o quê e quanto vai ser apagado antes de agir.

### 🔍 Arquivos duplicados
Encontra arquivos idênticos por conteúdo (comparação por hash, não só por nome), numa pasta específica ou na máquina inteira. Sugere manter o arquivo mais antigo e marca os demais — você revisa e decide o que excluir.

### 🗑️ Resíduos de programas desinstalados
Cruza as pastas de instalação (Program Files, AppData, ProgramData — inclusive de outras contas de usuário) com a lista de programas realmente instalados no Windows, e sinaliza pastas órfãs. Só marca como resíduo o que não é tocado há pelo menos 90 dias, pra não confundir uma ferramenta em uso ativo (cache de npm, IDEs, etc.) com lixo de verdade.

### 🚀 Desempenho
- Plano de energia (Equilibrado / Alto desempenho / Economia)
- Efeitos visuais: melhor aparência ↔ melhor desempenho
- Programas na inicialização — habilitar/desabilitar com um clique
- Serviços em segundo plano — lista curada e segura, com explicação de cada um
- Ajustes avançados: priorizar o programa em uso, agendamento de GPU por hardware, hibernação

### 💾 Espaço em disco
Escaneia uma unidade inteira de uma vez (com barra de progresso e opção de cancelar) e depois disso navegar entre pastas é **instantâneo** — sem recalcular nada a cada clique, ao estilo WinDirStat. Mostra os maiores consumidores de espaço e permite excluir direto.

---

## Como foi pensado

- **Nunca apaga nada sem confirmação explícita** — toda exclusão mostra antes o que e quanto vai ser removido.
- **Detecta automaticamente quando precisa de administrador** e avisa, com um botão pra reiniciar elevado quando necessário (Windows\Temp, Prefetch, serviços, outras contas de usuário, etc.).
- **Notificação nativa do Windows** quando uma tarefa demorada termina em segundo plano, e indicador animado no menu mostrando qual aba ainda está trabalhando.
- Todas as varreduras rodam fora da thread de interface — a janela não trava enquanto processa.

## Tecnologia

- .NET 8 / WPF (C#), padrão MVVM com [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- Sem dependências externas de infraestrutura — usa só APIs nativas do Windows (registro, WMI, powercfg, Service Control Manager)

## Como rodar

```
dotnet build
dotnet run --project src/OtimizaPC
```

Ou publique um executável único e independente:

```
dotnet publish src/OtimizaPC -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Algumas funcionalidades (limpar `Windows\Temp`, Prefetch, mexer em serviços, acessar pastas de outras contas de usuário) exigem executar como administrador — o programa detecta isso automaticamente e oferece reiniciar elevado quando necessário.

## ⚠️ Aviso

Este programa mexe em arquivos, registro e serviços do Windows. Embora ele
sempre mostre o que vai ser alterado/apagado antes de agir, o uso é por sua
conta e risco — faça backup do que for importante antes de usar,
especialmente na primeira vez. O software é distribuído "no estado em que
se encontra" (AS IS), sem garantias de nenhum tipo, e o autor não se
responsabiliza por eventuais danos ou perdas decorrentes do uso. Veja a
licença ([LICENSE](LICENSE)) para os termos completos.
