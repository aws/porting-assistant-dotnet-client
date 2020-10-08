![Porting Assistant for .NET](./logo.png "Porting Assistant for .NET")

# Porting Assistant for .NET
[![nuget](https://img.shields.io/nuget/v/PortingAssistant.Client.svg)](https://www.nuget.org/packages/PortingAssistant.Client/)

Porting Assistant for .NET is an analysis tool that scans .NET Framework applications and generates a .NET Core compatibility assessment, helping customers port their applications to Linux faster.

Porting Assistant for .NET quickly scans .NET Framework applications to identify incompatibilities with .NET Core, finds known replacements, and generates detailed compatibility assessment reports. This reduces the manual effort involved in modernizing the applications to Linux.

[PortingAssistant.Client](https://www.nuget.org/packages/PortingAssistant.Client/) package provides interfaces to analyze .NET applications for finding the incompatibilities, and port applications to .NET Core. Please note that,currently, limited support is there for porting.

For more information on Porting Assistant tool and try the tool, please refer the documenation: https://aws.amazon.com/porting-assistant-dotnet/

# Getting Started

Follow the examples below to see how the library can be integrated into your application.

```csharp
/* 1. Logger object */
   Log.Logger = new LoggerConfiguration()
       .MinimumLevel.Debug()
       .WriteTo.Console(theme: AnsiConsoleTheme.Code)
       .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
       .CreateLogger();

/* 2. Create Configuration settings */
AnalyzerConfiguration configuration = new AnalyzerConfiguration(LanguageOptions.CSharp)
{
    MetaDataSettings =
    {
        LiteralExpressions = true,
        MethodInvocations = true
    }
};

/* 3. Get Analyzer instance based on language */
CodeAnalyzer analyzer = CodeAnalyzerFactory.GetAnalyzer(configuration, Log.Logger);
            
/* 4. Analyze the project or solution */
var analyzerResults = await analyzer.AnalyzeSolution(wsPath);
```

## Getting Help

Please use these community resources for getting help. We use the GitHub issues
for tracking bugs and feature requests.

* Send us an email to: aws-porting-assistant-support@amazon.com
* If it turns out that you may have found a bug,
  please open an [issue](https://github.com/aws/porting-assistant-dotnet-client/issues/new)
  
## Permissions: AWS Identity and Access Management (IAM)

You must attach the following IAM policy as an inline policy to your IAM user. Then, configure a profile on your server with the IAM credentials of this user.


```javascript
AWS Identity and Access Management (IAM)

You must attach the following IAM policy as an inline policy to your IAM user. Then, configure a profile on your server with the IAM credentials of this user.

{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "EnCorePermission",
            "Effect": "Allow",
            "Action": [
                "execute-api:invoke",
                "s3:GetObject",
                "s3:ListBucket"
            ],
            "Resource": [
                "arn:aws:execute-api:us-east-1:492443789615:3dmmp07yx6/*",
                "arn:aws:execute-api:us-east-1:547614552430:8q2itpfg51/*",
                "arn:aws:s3:::aws.portingassistant.dotnet.datastore",
                "arn:aws:s3:::aws.portingassistant.dotnet.datastore/*"
            ]
        }
    ]
}
```

## Contributing

We welcome community contributions and pull requests. See
[CONTRIBUTING](./CONTRIBUTING.md) for information on how to set up a development
environment and submit code.

# Additional Resources

[Porting Assistant for .NET]  https://docs.aws.amazon.com/portingassistant/index.html

[AWS Developer Center - Explore .NET on AWS](https://aws.amazon.com/developer/language/net/)
Find all the .NET code samples, step-by-step guides, videos, blog content, tools, and information about live events that you need in one place.

[AWS Developer Blog - .NET](https://aws.amazon.com/blogs/developer/category/programing-language/dot-net/)
Come see what .NET developers at AWS are up to!  Learn about new .NET software announcements, guides, and how-to's.

[@awsfornet](https://twitter.com/awsfornet)
Follow us on twitter!

# License

Libraries in this repository are licensed under the Apache 2.0 License.

See [LICENSE](./LICENSE) and [NOTICE](./NOTICE) for more information.  

