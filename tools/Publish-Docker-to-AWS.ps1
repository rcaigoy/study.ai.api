# Parameter declaration

[CmdletBinding()]
param(
    [string]$AWSProfile="VS2022-us-west-1",
    [string]$OptionalVerSuffix=""
)
$aws_account_and_region = "007289006258.dkr.ecr.us-west-1.amazonaws.com"
$ecr_repo_name = "quizcraft-api-ecr"
$aws_region = "us-west-1"

# Build the project first in Release mode so we can get the Version #
cd ..\study.ai.api
dotnet build "study.ai.api.csproj" -c Release
$exePath = Join-path ".\bin\Release\net8.0" "study.ai.api.exe"
$fileInfo = (Get-ChildItem $exePath)[0]
$verInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($fileInfo)
if ($verInfo.FilePrivatePart -gt 0) {
    $verNo = $verInfo.FileVersion
}
else {
    $verNo="$($verInfo.FileMajorPart).$($verInfo.FileMinorPart).$($verInfo.FileBuildPart)";
}
$verNo += $OptionalVerSuffix;

$ecr_repo_with_verno = "$($ecr_repo_name):$($verNo)";

# Build the docker image using the command:
docker build --progress plain -f Dockerfile --build-arg NUGET_GITHUB_PACKAGES_USER=$Env:NUGET_GITHUB_PACKAGES_USER --build-arg NUGET_GITHUB_PACKAGES_TOKEN=$Env:NUGET_GITHUB_PACKAGES_TOKEN -t $ecr_repo_with_verno ..
# Create a tag for the remote using the command:
docker tag $ecr_repo_with_verno $aws_account_and_region/$ecr_repo_with_verno
# Login to the AWS ECR Repo using the command:
aws ecr get-login-password --profile $AWSProfile --region $aws_region | docker login --username AWS --password-stdin https://$aws_account_and_region
# Push the image to the ECR Repo using the command:
docker push $aws_account_and_region/$ecr_repo_with_verno
# Verify that the image with the specified VersionNo as the tag exists in the AWS target region using the command:
aws ecr describe-images --profile $AWSProfile --region $aws_region --repository-name $ecr_repo_name --image-ids imageTag=$verNo

cd ..\tools
