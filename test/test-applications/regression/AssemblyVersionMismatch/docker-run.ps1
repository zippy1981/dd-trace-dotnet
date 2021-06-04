Param(
    [Parameter(Mandatory = $true)]
    [String]
    $dockerfile
)

$tag = [System.IO.Path]::GetFileNameWithoutExtension($dockerfile)
$dockerfile = [System.IO.Path]::GetFileName($dockerfile)
docker build -f $dockerfile -t $tag ../../../..
# docker run --rm $tag
