# Modern Distributed Tracing in .NET

<a href="https://www.packtpub.com/product/modern-distributed-tracing-in-net/9781837636136?utm_source=github&utm_medium=repository&utm_campaign="><img src="https://content.packt.com/B19423/cover_image_small.jpg" alt="Modern Distributed Tracing in .NET" height="256px" align="right"></a>

This is the code repository for [Modern Distributed Tracing in .NET](https://www.packtpub.com/product/modern-distributed-tracing-in-net/9781837636136?utm_source=github&utm_medium=repository&utm_campaign=), published by Packt.

**A practical guide to observability and performance analysis for microservices**

## What is this book about?
Distributed tracing provides a methodical way of identifying and debugging performance and functional issues in complex systems. This book shows you how to use distributed tracing in practice along with metrics, correlated logs, and .NET diagnostic tools, and resolve production issues faster.

This book covers the following exciting features:
* Understand the core concepts of distributed tracing and observability
* Auto-instrument .NET applications with OpenTelemetry
* Manually instrument common scenarios with traces and metrics
* Systematically debug issues and analyze the performance
* Keep performance overhead and telemetry volume under control
* Adopt and evolve observability in your organization

If you feel this book is for you, get your [copy](https://www.amazon.com/dp/1837636133) today!

<a href="https://www.packtpub.com/?utm_source=github&utm_medium=banner&utm_campaign=GitHubBanner"><img src="https://raw.githubusercontent.com/PacktPublishing/GitHub/master/GitHub.png" 
alt="https://www.packtpub.com/" border="5" /></a>

## Instructions and Navigations
All of the code is organized into folders. For example, Chapter02.

The code will look like the following:
```
using var activity = Source.StartActivity("DoWork");
try
{
   await DoWorkImpl(workItemId);
}
catch
{
   activity?.SetStatus(ActivityStatusCode.Error);
}
```

**Following is what you need for this book:**
This book is for software developers, architects, and systems operators running .NET services who want to use modern observability tools and standards and take a holistic approach to performance analysis and end-to-end debugging. Software testers and support engineers will also find this book useful. Basic knowledge of the C# programming language and .NET platform is assumed to grasp the examples of manual instrumentation, but it is not necessary.

With the following software and hardware list you can run all code files present in the book (Chapter 1-15).
### Software and Hardware List
| Chapter | Software required | OS required |
| -------- | ------------------------------------ | ----------------------------------- |
| 1-15 | .NET SDK 7.0 | Windows, Mac OS X, and Linux (Any) |
| 1-15 | OpenTelemetry for .NET version 1.4.0 | Windows, Mac OS X, and Linux (Any) |
| 1-15 | Docker and docker-compose tools | Windows, Mac OS X, and Linux (Any) |
| 15 | .NET Framework 4.6.2  | Windows |
| 4 | PerfView tool | Windows cross-platform alternatives are available  |


We also provide a PDF file that has color images of the screenshots/diagrams used in this book. [Click here to download it]( https://packt.link/BBBNm).

### Related products
* .NET MAUI for C# Developers [[Packt]](https://www.packtpub.com/product/net-maui-for-c-developers/9781837631698?utm_source=github&utm_medium=repository&utm_campaign=) [[Amazon]](https://www.amazon.com/dp/837631697)

* Enterprise Application Development with C# 10 and .NET 6 - Second Edition [[Packt]](https://www.packtpub.com/product/enterprise-application-development-with-c-10-and-net-6-second-edition/9781803232973?utm_source=github&utm_medium=repository&utm_campaign=) [[Amazon]](https://www.amazon.com/dp/1801077363)



## Get to Know the Author
**Liudmila Malkova**
is a principal software engineer working on observability at Microsoft. She is a co-author of the distributed tracing in .NET and tracing implementation in Azure Functions and Application Insights SDK features. She’s currently a tracing and observability architect on Azure SDKs and an active contributor to the OpenTelemetry semantic conventions and instrumentation working group.
Liudmila’s love to observability started at Skype, where she got first-hand experience running complex systems at high scale and was fascinated by how much telemetry can reveal even to those deeply familiar with the code.




