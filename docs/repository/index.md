This overview page serves as your guide to navigating the exciting world of Sotex-box's codebase. Whether you're a seasoned developer joining the team or a curious explorer, we aim to provide a clear understanding of our project's structure and its inner workings.

## Repository layout

The Sotex-box repository is organized into well-defined directories, each with a specific purpose:
<!-- To update the list bellow run in root directory:
tree . -L 1 -a
Remember to remove .gitignored files if they render-->
```bash
.
├── .config # Folder that contains config files, e.g. config for dotnet tools
├── .dockerignore # File that contains all the things that docker should include in the build process
├── .editorconfig # File that specifies how should files be formated
├── .github # Folder with workflows
├── .pre-commit-config.yaml # Precommit configuration
├── CODEOWNERS # File containing the codeowners of certain parts of the repository
├── Makefile # Make file that contains some quick-access shortcuts maintained by the team
├── README.md # Readme that is rendered for github repository
├── android # Folder that contains android code that we use
├── distribution # Folder containing images for our stack
├── docker-compose.yaml # Development stack
├── docs # Folder containing all the documentation that is used to power this site
├── dotnet # Folder containing all the dotnet code we use
├── infra # Folder containing all IAAC code we use
├── mkdocs.yaml # Configuration file for this site
├── poetry.lock # Lock of dependencies for poetry project
├── pyproject.toml # Poetry project specification
├── requirements.txt # Autoexported file for python used for docker images based of scripts
└── sotex-box.sln # Solution file that manages dotnet code
```

## Code-Related information
Some random facts about the code:

* Each change should be carried out by a PR into a `main` branch.
* For each PR write a meaningful description. You will be provided with a pull request template which you should extend.
* Take your time with the PR. Don't skip and don't rush. We emphasize on quality rather than quantity.
* Add tests. From unit and integration, to end-to-end and smoke tests.
* Use precommit hooks. They will help you detect problems with your code even before you push the code.
* There is CI pipelines setup which ensure code quality and automate deployments. Their passing is a must for any PR to be accepted.

## Getting started
To kickstart your journey with Sotex-box development:

1. Clone the repository
2. Setup your environment: Follow instructions from [Working with repository](/repository/working-with-repository.html) guide
3. Run the tests: Verify the project's functionallity by running the tests
```bash
make dotnet-tests
```
4. Explore the code: Dive into the source code and find out more about parts that are of your interest
5. Contribute: Make a branch, follow [code-related](#code-related-information) and merge your changes

We hope this overview serves as a valuable starting point for your exploration of the Sotex-box codebase. We  encourage you to actively contribute and help us push the boundaries of this exciting project!
