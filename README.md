<p align="center">
  <a href="https://grandnode.com/">
    <img src="https://grandnode.com/logo.png" alt="GrandNode - Open Source E-Commerce Platform">
  </a>

  <h1 align="center">OPEN-SOURCE E-COMMERCE PLATFORM
    <br />
    FREE, FAST, FLEXIBLE, FEATURE-RICH</h1>
     <p align="center">
    GrandNode is a powerful, scalable e-Commerce platform built with MongoDB and ASP.NET Core. <br />
Based on the modern MongoDB database, this fully open-source system supports multiple business models: <br />
  B2B, B2C, Multi-Store, Multi-Vendor, Multi-Tenant, Multi-Language, Multi-Currency. <br />
Achieve superior performance, unlimited scalability, and comprehensive customization to drive your online business success.
  </p>
  <p align="center">
    <a href="https://grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme"><strong>Explore the project »</strong></a>
    <br />
    <br />
    <a href="https://demo.grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme">View Demo</a>
    ·
    <a href="https://github.com/grandnode/grandnode2/issues">Report Bug</a>
    ·
    <a href="https://github.com/grandnode/grandnode2/issues">Request Feature</a>
    ·
    <a href="https://grandnode.com/boards/?utm_source=github&utm_medium=link&utm_campaign=readme">Visit forum</a>
    ·
    <a href="https://grandnode.com/grandnode-themes/?utm_source=github&utm_medium=link&utm_campaign=readme">Themes</a>
    ·
    <a href="https://grandnode.com/extensions/?utm_source=github&utm_medium=link&utm_campaign=readme">Integrations & Plugins</a>
    ·
    <a href="https://grandnode.com/premium-support-packages/?utm_source=github&utm_medium=link&utm_campaign=readme">Premium support</a>
  </p>
</p>
<div align="center">

![Tests on Linux, MacOS and Windows](https://github.com/grandnode/grandnode2/actions/workflows/aspnetcore.yml/badge.svg)
[![Build Status](https://dev.azure.com/grandnode/grandnode2/_apis/build/status/grandnode.grandnode2?branchName=main)](https://dev.azure.com/grandnode/grandnode2/_build/latest?definitionId=8&branchName=main)
[![Docker Image CI](https://github.com/grandnode/grandnode2/actions/workflows/docker-image.yml/badge.svg)](https://github.com/grandnode/grandnode2/actions/workflows/docker-image.yml)
![License](https://img.shields.io/github/license/grandnode/grandnode2)
[![CodeQL Advanced](https://github.com/grandnode/grandnode2/actions/workflows/codeql.yml/badge.svg)](https://github.com/grandnode/grandnode2/actions/workflows/codeql.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=grandnode_grandnode2&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=grandnode_grandnode2)
<a href="https://docs.grandnode.com/"><img src="https://img.shields.io/badge/Docs-docs.grandnode.com-brightgreen"></a>
</div>

<!-- TABLE OF CONTENTS -->
## Table of Contents

* [Overview](#Overview)
* [Key Features](#key-features)
* [Technical Highlights](#technical-highlights)
* [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Installation](#installation)
  * [Online demo](#online-demo)
* [Roadmap](#roadmap)
* [Contributing](#contributing)
* [Sponsors](#sponsors)
* [Why Choose GrandNode?](#why-choose-grandnode)
* [License](#license)


## Overview

GrandNode was designed to solve the most important business challenges from the world of digital shopping. The goal for us is to provide the platform with:
* The high performance front-end, rendered within miliseconds,
* The high performance application to handle temporary and permanent traffic overloads,
* Highly advanced e-commerce platform with unlimited possibilities of integration with existing third-party softwares
* Fast development with modern codebase
* Scalable e-commerce platform to grow with the business

## Key Features

### Performance & Architecture
- ⚡ **High-Performance** - Pages render in milliseconds
- 📊 **MongoDB Database** - Superior scalability and performance
- 🚀 **ASP.NET Core** - Modern and efficient codebase

### Business Features
- 🏪 **Multi-Store Management** - Run multiple stores from one installation
- 👥 **B2B & B2C Support** - Serve both business and consumer customers
- 🌎 **Multi-Language & Multi-Currency** - Sell globally with localized experiences
- 🛒 **Advanced Product Catalog** - Flexible product attributes, variants, and pricing
- 💰 **Multiple Payment Gateways** - Including Stripe, BrainTree and more
- 🚚 **Customizable Shipping Options** - Fixed rate, by weight, shipping points
- 📱 **Mobile-Optimized** - Responsive design for all devices

### Marketing & SEO
- 🔍 **SEO-Friendly** - URL structure, meta tags, and sitemap generation
- 🔔 **Customer Segmentation** - Target specific customer groups
- 📧 **Email Marketing Integration** - Boost your sales with newsletters
- 📊 **Analytics Integration** - Track performance with Google Analytics

## Technical Highlights

GrandNode 2 leverages the latest technologies to deliver a high-performance e-commerce solution:

- **ASP.NET Core 9.0** - Modern, cross-platform framework
- **MongoDB 4.0+** - NoSQL database for unlimited scalability
- **Docker Support** - Easy deployment and containerization
- **REST API** - Comprehensive API for integrations
- **Cloud-Ready** - Optimized for cloud hosting environments
- **Real-time Processing** - Immediate updates throughout the system

<!-- GETTING STARTED -->
## Getting Started

To get a local copy up and running follow these simple steps.

### Prerequisites (develop version)

GrandNode requires .NET Core 9.0, MongoDB 4.0+, and OS-specific dependency tools. 

### Installation

GrandNode can be installed in a few different ways. Note: The develop branch is the development version of GrandNode and it may be unstable. The main branch is the primary branch that contains the latest stable version. You can also download specific stable versions from the Releases page or switch to a release branch.

* Docker 
```bash
docker run -d -p 127.0.0.1:27017:27017 --name mongodb mongo 
docker run -d -p 80:8080 --name grandnode2 --link mongodb:mongo -v grandnode_images:/app/wwwroot/assets/images -v grandnode_appdata:/app/App_Data grandnode/grandnode2
``` 
If you want to download the latest stable version of GrandNode please use the following command, where x.xx is a number of GrandNode release: 
```bash
docker pull grandnode/grandnode2:x.xx 
```

* Open locally with VS2022+ (v17.12.0) or above

Run the project in the Visual Studio 2022+, extract the source code package downloaded from Releases tab to a folder. Enter the extracted folder and double-click the GrandNode.sln solution file. Select the Plugins project, rebuild it, then select the GrandNode.Web project.

* Host on Linux server 

Before you start - please install, configure the nginx server, .NET Core 9.0+ and MongoDB 4.0+
```bash
mkdir ~/source
cd ~/source
git clone - b x.xx https://github.com/grandnode/grandnode2.git
```
```bash
cd ~/source/grandnode
dotnet restore GrandNode.sln
```
Now it's time to rebuild all of our plugins and publish application (command is pretty long because we've combined all commands in a single line, to ease up your work):
```bash
sudo dotnet build src/Plugins/Authentication.Facebook && sudo dotnet build src/Plugins/Authentication.Google && sudo dotnet build src/Plugins/DiscountRules.Standard && sudo dotnet build src/Plugins/ExchangeRate.McExchange && sudo dotnet build src/Plugins/Payments.BrainTree && sudo dotnet build src/Plugins/Payments.CashOnDelivery && sudo dotnet build src/Plugins/Payments.StripeCheckout && sudo dotnet build src/Plugins/Shipping.ByWeight && sudo dotnet build src/Plugins/Shipping.FixedRateShipping && sudo dotnet build src/Plugins/Shipping.ShippingPoint && sudo dotnet build src/Plugins/Tax.CountryStateZip && sudo dotnet build src/Plugins/Tax.FixedRate && sudo dotnet build src/Plugins/Widgets.FacebookPixel && sudo dotnet build src/Plugins/Widgets.GoogleAnalytics && sudo dotnet build src/Plugins/Widgets.Slider && sudo dotnet build src/Plugins/Theme.Modern && sudo dotnet publish src/Web/Grand.Web -c Release -o /var/webapps/grandnode 
```
Optional: Create the service file, to automatically restart your application.
```bash
sudo vi /etc/systemd/system/grandnode.service
```
Paste the following content, and save changes:
```ini
[Unit]
Description=GrandNode

[Service]
WorkingDirectory=/var/webapps/grandnode
ExecStart=/usr/bin/dotnet /var/webapps/grandnode/Grand.Web.dll
Restart=always
RestartSec=10
SyslogIdentifier=dotnet-grandnode
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```
Enable the service and restart the GrandNode
```
sudo systemctl enable grandnode.service
sudo systemctl start grandnode.service
``` 
Feel free to visit our [detailed guide about GrandNode installation.](https://grandnode.com/how-to-install-grandnode-on-linux-ubuntu-1604/?utm_source=github&utm_medium=link&utm_campaign=readme)

### Online demo 
#### Frontend #### 
[https://demo.grandnode.com/](https://demo.grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme)

#### Backend #### 
[https://demo.grandnode.com/admin](https://demo.grandnode.com/admin/?utm_source=github&utm_medium=link&utm_campaign=readme) 


Demo is restoring once per day to the original state. 

Access to the admin panel:

Admin email: admin@yourstore.com 

Admin password: 123456


## Roadmap

We have a clear vision in which direction we would like to develop GrandNode. Ready roadmaps with milestones for future versions of GrandNode can be found in the [projects tab](https://github.com/grandnode/grandnode2/projects).


## Contributing

GrandNode is and always will be free and open-source.
How to contribute:
- Star this project on GitHub.
- Report bugs or suggest features by creating new issues
- Submit pull requests
- Become a sponsor and donate to us

## Sponsors

Become a sponsor and get your logo on our README on Github with a link to your site. [[Become a sponsor](https://opencollective.com/grandnode#sponsor)]

## Why Choose GrandNode?

GrandNode stands out in the crowded e-commerce platform market by offering:

- **Superior Performance** - MongoDB and ASP.NET Core ensure lightning-fast page loads
- **Ultimate Scalability** - From startup to enterprise, grow without limits
- **Lower Total Cost of Ownership** - Free, open-source with no licensing costs
- **Modern Technology Stack** - Built with future-proof technologies
- **Extensible Architecture** - Build custom modules and integrations

Whether you're launching a single online store or building a complex multi-vendor marketplace, GrandNode provides the tools and performance you need to succeed in today's competitive e-commerce landscape.

## Code of conduct

To clarify behavior rules in our community, GrandNode has adopted the code of conduct defined by the Contributor Covenant. For more information see the [Code of Conduct.](https://www.contributor-covenant.org/version/2/0/code_of_conduct/)

## License
GrandNode is completely free and distributed under the GNU General Public License v3.0. It's available [here](LICENSE)