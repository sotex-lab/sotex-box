From the day one we've invested time into setting up continous integration and continous deployment strategies to ensure a smooth sailing process from development to staging to production. There are multiple things that happen if you change code in some way and these changes aim to help you automate the transition from one environment to another ensuring greater success rate and making those processes as transparent as possible.

## Dotnet checks
Defined in `.github/workflows/dotnet.yaml` we check multiple things related to code quality, unit testing, integration testing and getting notified of anything failing as soon as possible.
!!! tip "Code quality tips & tricks"
    If you want to ensure that your code passes code quality requirements be sure to enable `pre-commit` by installing it! To view the full process on how to setup your repository to ensure success follow the ["Working with repository"](/repository/working-with-repository.html) guide! Some things that can fail on CI are already **automatically** fixable with these.

Apart from assuring code quality and test passage, this pipeline ensures that we can build a [docker image](https://www.techtarget.com/searchitoperations/definition/Docker-image) from the changes you've introduced. Be aware that if the merge request doesn't involve any changes to the stack related to the docker images it won't trigger the build.

Right now we are using [alipne linux](https://www.alpinelinux.org/) as operating system to base our images off of which results in images being really small.

On top of that, when your code gets merged into `main` branch we will automatically push the image to our repositories packages. You can find all the exporter packages on [ghcr](https://github.com/orgs/sotex-lab/packages?repo_name=sotex-box). After having an image tagged and built we will go ahead and update staging with the new version we built in ci.

!!! bug "TODO"
    Extend this once we have production setup and have a process in place.
