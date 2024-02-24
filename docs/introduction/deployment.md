The deployment process of the whole stack depends on multiple individual pieces of deployment process. You will rarely (if ever) have to perform all of these at once. Furthermore there is automation that will cover a lot of these things for you. To exaplin the whole process we have to distinguish between:

1. [Deploying infrastructure to cloud provider](#deploying-infrastructure-to-cloud-provider)
2. [Deploying backend api to the chosen infrastructure](#deploying-backend-api-to-the-chosen-infrastructure)
3. [Deploying the frontend app to google store](#deploying-the-frontend-app-to-google-store)

## Deploying infrastructure to cloud provider

At the moment we have chosen to use [AWS](https://aws.amazon.com/serverless/) as our cloud provider and our pulumi config is implemented to support that. To go in a little bit more detail we have chosen the [AWS Fargate](https://aws.amazon.com/fargate/?c=ser&sec=srv) where we host our containers. The complete architecture can be seen [here](/introduction/software-arch.html).

After making changes to the `infra/backend` project, be it the configuration changes in `Pulumi(.<env>).yaml` or adding and removing resources in `Program.cs` you should follow the same approach we have for all other changes:

1. Make a separate branch
2. Make a commit and push the branch
3. Wait for the checks to pass
4. Ask for a review and merge the code

Once the code gets merged to the main branch your changes will be successfully pushed to `staging` environment.

??? TODO
    Explain the process for deploying on `production`. There should be automation handling this

## Deploying backend api to the chosen infrastructure
??? TODO
    Explain the process of deploying the backend api to cloud. There should be automation handling this

## Deploying the frontend app to google store
??? TODO
    Explain the process of deploying the frontend app to google store in high level. There should be automation handling this
