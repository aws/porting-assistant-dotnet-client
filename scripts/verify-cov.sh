#!/bin/sh -e
rm -rf ./commit-hook-tests
dotnet test --collect:"XPlat Code Coverage" --settings scripts/coverlet.runsettings PortingAssistantClient/PortingAssistantClient.sln -r ./commit-hook-tests
for f in ./commit-hook-tests/**/*.xml
do
  echo ""
  echo "Code Coverage Check: $f"

  coverage=`cat $f | grep "<coverage" | cut -d" " -f 2 | cut -d"=" -f 2 | cut -d"\"" -f 2`
  if [[ (($coverage < 0.20)) ]]
  then
    echo "Code coverage requirement not met. Expected: > 0.20, Actual: $coverage"
    exit 1
  fi
  echo "Code coverage: $coverage"
done
rm -rf ./commit-hook-tests