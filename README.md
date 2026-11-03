# Traz Base

Aplicação desktop para Windows que copia arquivos de solicitações, extrai pacotes e prepara bases Firebird por uma interface visual.

## Requisitos

- Windows 10 ou superior.
- .NET 10 SDK para compilar e executar pelo projeto.
- Serviço Firebird local instalado e ativo para acessar os arquivos `.FDB`.
- Permissão de leitura na pasta de origem e permissão de gravação na pasta de destino.

O instalador autocontido para Windows x64 inclui o runtime do .NET e não exige uma instalação separada do runtime. O serviço Firebird local precisa estar instalado e ativo para as operações de banco de dados.

## Executar durante o desenvolvimento

```powershell
dotnet run --project TrazBase.App/TrazBase.App.csproj
```

## Compilar

```powershell
dotnet build TrazBase.App/TrazBase.App.csproj -c Release
```

Para gerar o pacote autocontido para Windows x64:

```powershell
dotnet publish TrazBase.App/TrazBase.App.csproj -c Release -r win-x64 --self-contained true -o Instalador/Aplicativo
```

O conteúdo de `Instalador/Aplicativo` e os instaladores são artefatos de distribuição e ficam fora do Git. Publique `TrazBase-Setup.exe` como asset de uma GitHub Release para que a aplicação detecte e instale automaticamente a versão mais recente.

Para criar o instalador executável com o Inno Setup instalado, execute no Windows:

```powershell
powershell -ExecutionPolicy Bypass -File Instalador/Compilar-Instalador.ps1
```

O script usa `Instalador/TrazBase.iss` e gera `Instalador/TrazBase-Setup.exe`. O destino padrão é `C:\Program Files\TrazBase`; o instalador solicita privilégios de administrador e permite escolher outra pasta.

As atualizações iniciadas pela aplicação executam o instalador em modo silencioso, sem telas de instalação. O Windows ainda solicita autorização do UAC para atualizar arquivos em `Program Files`.

Para lançar uma atualização, incremente `<Version>` em `TrazBase.App/TrazBase.App.csproj` e `AppVersion`/`AppFileVersion` em `Instalador/TrazBase.iss`, publique novamente em `Instalador/Aplicativo` e recompile o instalador. Ao executar sobre uma instalação existente, o setup bloqueia versões antigas, mas permite reinstalar a versão atual para aplicar correções sem mudar o número da versão. Os arquivos da aplicação são substituídos; arquivos sem versão (JSON, PDB, SVG e ícones) são atualizados quando o horário de modificação muda.

## Configuração e privacidade

Configure a pasta de solicitações, as pastas locais e a senha Firebird na própria aplicação. Não há caminhos de rede da empresa definidos como padrão. As preferências e os comandos SQL ficam no perfil local do usuário em `%APPDATA%\TrazBase`; quando solicitada, a senha é protegida pelo DPAPI do Windows para a conta atual.

O script operacional legado foi excluído deste repositório porque contém credenciais e referências internas. Não o force para o Git nem publique cópias desse arquivo.

## Funcionalidades

- Cópia com progresso e cancelamento.
- Extração de ZIP nativa e de RAR pela biblioteca SharpCompress.
- Edição e teste de comandos SQL em bases ARQEMP e ARQSIST.
- Consulta e exibição da versão do banco ARQSIST.
- Preferência de tema claro/escuro persistida.
- Instalador que permite escolher a pasta de instalação.
- Exibição da versão e aviso de atualização a partir das GitHub Releases.

## Documentação

- [Requisitos](docs/REQUISITOS.md)
- [Padrões de interface](docs/PADROES-DE-INTERFACE.md)
- [Fluxos](docs/FLUXOS.md)
- [Arquitetura](docs/ARQUITETURA.md)
- [Plano](docs/PLANO.md)
- [Riscos e decisões](docs/RISCOS-E-DECISOES.md)
