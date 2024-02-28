There are two main options:

* Dev environment - with docker
* Local tools

### Dev environment - with docker

!!! bug "TODO"
    explain how to work with docker dev environment once its setup

### Local tools
You need a couple of tools to be able to fully work with this repository:

!!! tip "Tip for windows users"
    Even if you are using windows to get the best development experience its advised to use [WSL](https://learn.microsoft.com/en-us/windows/wsl/install)!

| # | Name | Version | Windows | Linux | Mac |
|---|------|---------|---------|-------|-----|
| 1 | python | ^3.9 | [🔗](https://www.python.org/downloads/windows/) | [🔗](https://www.python.org/downloads/source/) | [🔗](https://www.python.org/downloads/macos/) |
| 2 | poetry | ^1.7.1 | [🔗](https://python-poetry.org/docs/#installing-with-the-official-installer) | [🔗](https://python-poetry.org/docs/#installing-with-the-official-installer) | [🔗](https://python-poetry.org/docs/#installing-with-the-official-installer) |
| 3 | precommit | ^3.6.0 | [🔗](https://pre-commit.com/#install) | [🔗](https://pre-commit.com/#install) | [🔗](https://pre-commit.com/#install) |
| 4 | dotnet | ^8.0.1 | [🔗](https://dotnet.microsoft.com/en-us/download) | [🔗](https://dotnet.microsoft.com/en-us/download) | [🔗](https://dotnet.microsoft.com/en-us/download) |
| 5 | pulumi | ^3.107.0 | [🔗](https://www.pulumi.com/docs/clouds/aws/get-started/begin/#install-pulumi) | [🔗](https://www.pulumi.com/docs/clouds/aws/get-started/begin/#install-pulumi) | [🔗](https://www.pulumi.com/docs/clouds/aws/get-started/begin/#install-pulumi) |
| 6a | docker | ^23.0.5 | [🔗](https://docs.docker.com/desktop/install/windows-install/) | [🔗](https://docs.docker.com/desktop/install/linux-install/) | [🔗](https://docs.docker.com/desktop/install/mac-install/) |
| 6b | podman | ^4.8.3 | [🔗](https://podman.io/docs/installation#windows) | [🔗](https://podman.io/docs/installation#installing-on-linux) | [🔗](https://podman.io/docs/installation#macos) |
| 7 | flutter | ^3.19.1 | [🔗](https://docs.flutter.dev/get-started/install/windows) | [🔗](https://docs.flutter.dev/get-started/install/linux) | [🔗](https://docs.flutter.dev/get-started/install/macos) |

We will maintain a `Makefile` where we will try to link as much actions as possible. Although some cases may be left uncovered.

Once you install the tools you should setup precommit hooks which help us maintain the code quality and runs tests on commit. Run the bellow command to do so.
```bash
pre-commit install
```

After that you can use `make` to run things from the repository. Some things that are not implemented require specific commands but those are a niece topic still.