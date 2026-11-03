# Traz Base

Aplicação desktop para Windows que copia arquivos de solicitações, extrai pacotes e prepara bases Firebird por uma interface visual.

## Requisitos

- Windows 10 ou superior.
- .NET 10 SDK para compilar e executar pelo projeto.
- Serviço Firebird local instalado e ativo para acessar os arquivos `.FDB`.
- Permissão de leitura na pasta de origem e permissão de gravação na pasta de destino.

O instalador autocontido para Windows x64 inclui o runtime do .NET e não exige uma instalação separada do runtime.

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

O conteúdo de `Instalador/Aplicativo` e o arquivo `TrazBase-Instalador.zip` são artefatos de distribuição e ficam fora do Git. Publique o ZIP em uma GitHub Release quando precisar distribuir uma versão.

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

## Documentação

- [Requisitos](docs/REQUISITOS.md)
- [Padrões de interface](docs/PADROES-DE-INTERFACE.md)
- [Fluxos](docs/FLUXOS.md)
- [Arquitetura](docs/ARQUITETURA.md)
- [Plano](docs/PLANO.md)
- [Riscos e decisões](docs/RISCOS-E-DECISOES.md)
