# Azure DevOps Extension Gate

## Background
When releasing Azure DevOps extensions via an automated CI/CD process there is a problem, from the moment it is uploaded to when the extension's tasks are available to a build agent, is not instantiation. The process can potentially take a few minutes to roll out. The problem this delay causes is a perfect candidate for using  Azure DevOps [Release Gates](https://docs.microsoft.com/en-us/azure/devops/pipelines/release/approvals/gates?view=azure-devops) or [Environment Checks](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/approvals?view=azure-devops); using the gate to make sure the expected version of a task is available to an agent before running the next stage of the CD pipeline e.g waiting after deploying a private build of an extension before trying to run functional tests.

To provide this check you can use an Azure Function. Historically I did created this via the Azure Portal, but in this repo I have moved the code to a project release via a YAML pipeline

## Using the Azure Function

In you Release add a gate or in your Environment add a check. 

- Set the URL parameter for the URL of the Azure Function. This value can be found from the Azure Portal. Note that you don’t need the Function Code query parameter in the URL as this is provided with the next gate parameter. I chose to use a variable group variable for this parameter so it was easy to reuse between many CD pipelines
- Set the Function Key parameter for the Azure Function, again you get this from the Azure Portal. This time I used a secure variable group variable
- Set the Method parameter to POST
- Set the Header content type as JSON
```
    {
        "Content-Type": "application/json"
    }
```
- Set the Body to contain the details of the Azure DevOps instance and Task to check. This time I used a mixture of variable group variables, release specific variables (the GUID) and environment build/release variables. The key here is I got the version from the primary release artifact $(BUILD.BUILDNUMBER) so the correct version of the tasks is tested for automatically
```
    {
        "instance": "$(instance)",
        "pat": "$(pat)",
        "taskguid": "$(taskGuid)",
        "version": "$(BUILD.BUILDNUMBER)"
    }
```
- Finally set  the Advanced/Completion Event to ApiResponse with the success criteria of
```
    eq(root['Deployed'], 'true')
```
Once this was done I was able to use the Azure function as a VSTS gate as required