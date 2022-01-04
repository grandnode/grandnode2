<p align="center">
  <a href="https://grandnode.com/">
    <img src="https://grandnode.com/logo.png" alt="Logo">
  </a>

  <h1 align="center">FREE, FAST, FLEXIBLE, FEATURE-RICH
    <br />
    OPEN-SOURCE E-COMMERCE PLATFORM</h1>
     <p align="center">
    GrandNode is an intuitive e-Commerce platform that enables the flexible development of online stores. <br />
Based on the modern MongoDB database, a fully free and open-source system allows you to support various business models: <br />
  B2B, B2C, Multi-Store, Multi-Vendor, Multi-Tenant, Multi-Language, Multi-Currency. <br />
Advanced personalization, scalability, speed, and high efficiency - these are features that effectively support your e-business management.
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
![Docker](https://img.shields.io/docker/cloud/build/grandnode/grandnode2)
![License](https://img.shields.io/github/license/grandnode/grandnode2)
<a href="https://docs.grandnode.com/"><img src="https://img.shields.io/badge/Docs-docs.grandnode.com-brightgreen"></a>
</div>

<!-- TABLE OF CONTENTS -->
## Table of Contents

* [Overview](#Overview)
* [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Installation](#installation)
  * [Online demo](#online-demo)
* [Awesome projects](#Awesome-projects)
* [Roadmap](#roadmap)
* [Contributing](#contributing)
* [Sponsors](#sponsors)
* [License](#license)


## Overview

GrandNode was designed to solve the most important business challenges from the world of digital shopping. The goal for us is to provide the platform with:
* The high performance front-end, rendered within miliseconds,
* The high performance application to handle temporary and permanent traffic overloads,
* Highly advanced e-commerce platform with unlimited possibilities of integration with existing third-party softwares
* Fast development with modern codebase
* Scalable e-commerce platform to grow with the business

<!-- GETTING STARTED -->
## Getting Started

To get a local copy up and running follow these simple steps.

### Prerequisites (develop version)

GrandNode requires .NET Core 6.0, MongoDB 4.0+, and OS-specific dependency tools. 

### Installation

GrandNode can be installed in a few different ways. Note: The develop branch is the development version of GrandNode and it may be unstable. To use the
latest stable version, download it from the Releases page or switch to a release branch. 

* Docker 
```
docker run -d -p 127.0.0.1:27017:27017 --name mongodb mongo 
docker run -d -p 80:80 --name grandnode2 --link mongodb:mongo grandnode/grandnode2
``` 
If you want to download the latest stable version of GrandNode please use the following command, where x.xx is a number of GrandNode release: 
```
docker pull grandnode/grandnode2:x.xx 
```

* Open locally with VS2022+

Run the project in the Visual Studio 2022+, extract the source code package downloaded from Releases tab to a folder. Enter the extracted folder and double-click the GrandNode.sln solution file. Select the Plugins project, rebuild it, then select the GrandNode.Web project.

* Host on Linux server 

Before you start - please install, configure the nginx server, .NET Core 6.0+ and MongoDB 4.0+
```
mkdir ~/source
cd ~/source
git clone - b x.xx https://github.com/grandnode/grandnode2.git
```
```
cd ~/source/grandnode
dotnet restore GrandNode.sln
```
Now it's time to rebuild all of our plugins and publish application (command is pretty long because we've combined all commands in a single line, to ease up your work):
```
sudo dotnet build src/Plugins/Authentication.Facebook && sudo dotnet build src/Plugins/Authentication.Google && sudo dotnet build src/Plugins/DiscountRules.Standard && sudo dotnet build src/Plugins/ExchangeRate.McExchange && sudo dotnet build src/Plugins/Payments.BrainTree && sudo dotnet build src/Plugins/Payments.CashOnDelivery && sudo dotnet build stc/Plugins/Payments.PayPalStandard && sudo dotnet build src/Plugins/Shipping.ByWeight && sudo dotnet build src/Plugins/Shipping.FixedRateShipping && sudo dotnet build src/Plugins/Shipping.ShippingPoint && sudo dotnet build src/Plugins/Tax.CountryStateZip && sudo dotnet build stc/Plugins/Tax.FixedRate && sudo dotnet build src/Plugins/Widgets.FacebookPixel && sudo dotnet build stc/Plugins/Widgets.GoogleAnalytics && sudo dotnet build src/Plugins/Widgets.Slider && sudo dotnet publish src/Web/Grand.Web -c Release -o /var/webapps/grandnode
```
Optional: Create the service file, to automatically restart your application.
```
sudo vi /etc/systemd/system/grandnode.service
```
Paste the following content, and save changes:
```
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

Install GrandNode with one click on [DigitalOcean](https://marketplace.digitalocean.com/apps/grandnode?refcode=8eafb78fb6ae)


### Online demo 
#### Frontend #### 
[https://demo.grandnode.com/](https://demo.grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme)

#### Backend #### 
[https://demo.grandnode.com/admin](https://demo.grandnode.com/admin/?utm_source=github&utm_medium=link&utm_campaign=readme) 


Demo is restoring once per day to the original state. 

Access to the admin panel:

Admin email: admin@yourstore.com 

Admin password: 123456


## Awesome projects

[![Awesome projects](https://grandnode.com/content/images/uploaded/Blog/awesomeprojectsgit1.JPG)](https://grandnode.com/showcase/?utm_source=github&utm_medium=link&utm_campaign=readme)

Check the [GrandNode Live Projects](https://grandnode.com/showcase/?utm_source=github&utm_medium=link&utm_campaign=readme).

Have you done something great with GrandNode? Let us know and get listed!


## Roadmap

We have a clear vision in which direction we would like to develop GrandNode. Ready roadmaps with milestones for future versions of GrandNode can be found in the [projects tab](https://github.com/grandnode/grandnode2/projects).


## Contributing

GrandNode is and always will be free and open-source.
How to contribut:
- Star this project on GitHub.
- Report bugs or suggest features by creating new issues
- Submit pull requests
- Become a sponsor and donate to us

## Sponsors

Become a sponsor and get your logo on our README on Github with a link to your site. [[Become a sponsor](https://opencollective.com/grandnode#sponsor)]

## Code of conduct

To clarify behavior rules in our community, GrandNode has adopted the code of conduct defined by the Contributor Covenant. For more information see the [Code of Conduct.](https://www.contributor-covenant.org/version/2/0/code_of_conduct/)

## License
GrandNode is completely free and distributed under the GNU General Public License v3.0. It's available [here](https://github.com/grandnode/grandnode2/blob/master/LICENSE)
