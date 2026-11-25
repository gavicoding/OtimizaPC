[🇺🇸 English](README.md) | **🇧🇷 Português**

# ⚡ OtimizaPC: limpeza segura do Windows

[![Windows Build](https://github.com/gavicoding/OtimizaPC/actions/workflows/build.yml/badge.svg)](https://github.com/gavicoding/OtimizaPC/actions/workflows/build.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20(MVVM)-0078D4.svg)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![C#](https://img.shields.io/badge/Linguagem-C%23-239120.svg)](https://learn.microsoft.com/dotnet/csharp/)
[![Licença: Apache-2.0](https://img.shields.io/badge/licen%C3%A7a-Apache_2.0-green.svg)](LICENSE)

Aplicativo para Windows, em tema escuro, que **libera espaço em disco e mostra a saúde do computador**. Feito em C# com .NET 8 e WPF.

---

## Para que serve

A maioria dos "otimizadores" apaga coisas sem mostrar o quê, mexe em configurações escondidas e às vezes quebra o sistema. O OtimizaPC foi feito ao contrário: **nada é apagado sem você ver antes**, e dá para criar um ponto de restauração com um clique antes de qualquer ajuste.

## O que você ganha

- **Espaço de volta sem medo:** cada limpeza mostra a lista e o tamanho exato do que vai sair.
- **Um jeito fácil de voltar atrás:** ponto de restauração do Windows com um clique.
- **Mais do que limpeza:** acha arquivos duplicados, restos de programas desinstalados e mostra o que ocupa espaço no disco.
- **O computador continua usável:** as varreduras rodam em segundo plano, sem travar a janela.

---

## O que ele faz

### 📊 Painel de saúde e ponto de restauração
- Uso de processador, memória e discos em tempo real, e **saúde do disco (S.M.A.R.T.)** quando aberto como administrador.
- Cria um **ponto de restauração do Windows** com um clique antes de qualquer mudança no sistema.

### 🧹 Limpeza segura (inclusive dos outros usuários)
Mostra o tamanho exato antes de apagar, em:
- arquivos temporários do usuário e do Windows, miniaturas e relatórios de erro;
- cache dos navegadores (Chrome, Edge, Firefox), **sem tocar em senhas, histórico ou favoritos**;
- Prefetch, cache do Windows Update, logs do sistema e sobras de atualizações antigas do Windows;
- Lixeira de todos os discos;
- **arquivos temporários dos outros usuários** do mesmo computador.

### 🔍 Arquivos duplicados de verdade
Compara o **conteúdo** dos arquivos (por hash), e não só o nome, numa pasta ou no computador inteiro. Por padrão mantém o arquivo mais antigo e deixa você revisar antes de apagar.

### 🗑️ Restos de programas desinstalados
Compara as pastas de instalação (`Program Files`, `AppData`, `ProgramData`, inclusive de outros usuários) com a lista de programas instalados. Só aponta pastas **paradas há mais de 90 dias**, para não marcar como lixo ferramentas que você usa (cache do `npm`, IDEs, SDKs).

### 🚀 Desempenho
- Troca do plano de energia (Equilibrado / Alto desempenho / Economia)
- Efeitos visuais (melhor aparência ou melhor desempenho)
- Programas que abrem com o Windows (liga e desliga com um clique)
- Lista de serviços em segundo plano com explicação simples do que é seguro mexer
- Ajustes avançados: prioridade para o programa em uso, agendamento de GPU por hardware e hibernação

### 💾 Mapa do que ocupa espaço
Varre o disco inteiro uma vez em segundo plano (com progresso e botão de cancelar) e guarda o resultado. Depois, abrir as subpastas é **instantâneo**, no estilo do WinDirStat, para achar o que está ocupando mais espaço.

---

## Como foi construído

- **Só recursos nativos do Windows:** Registro, WMI, `powercfg` e o gerenciador de serviços. Nada de servidor ou serviço externo.
- **Código organizado em MVVM:** telas (XAML), ViewModels ([CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)) e serviços (`CleanupService`, `DuplicateFinderService`, `DiskSpaceScanService`, `ResidueService` e outros).
- **Pede administrador só quando precisa:** percebe quando uma ação exige elevação (`Windows\Temp`, Prefetch, serviços, limpeza de outros usuários) e oferece reabrir como administrador com um clique.
- **Avisos ao terminar:** notificações do Windows quando uma tarefa longa acaba e indicadores na barra lateral enquanto ela roda.

---

## Como compilar e rodar

### Pré-requisito
- [SDK do .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) no Windows

### Rodar a partir do código

```bash
dotnet build OtimizaPC.sln
dotnet run --project src/OtimizaPC
```

### Gerar um único .exe que roda sem instalar o .NET

```bash
dotnet publish src/OtimizaPC -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

---

## Aviso

Este aplicativo mexe em arquivos do sistema, no Registro e em serviços do Windows. Ele sempre mostra uma prévia antes de apagar ou mudar algo, mas use por sua conta e mantenha backup do que é importante. Distribuído "como está", sem garantia. Veja os termos completos em [LICENSE](LICENSE).
