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
[![Build Status](https://dev.azure.com/grandnode/grandnode2/_apis/build/status/grandnode.grandnode2?branchName=develop)](https://dev.azure.com/grandnode/grandnode2/_build/latest?definitionId=8&branchName=develop)
[![Docker Image CI](https://github.com/grandnode/grandnode2/actions/workflows/docker-image.yml/badge.svg)](https://github.com/grandnode/grandnode2/actions/workflows/docker-image.yml)
![License](https://img.shields.io/github/license/grandnode/grandnode2)
[![CodeQL Advanced](https://github.com/grandnode/grandnode2/actions/workflows/codeql.yml/badge.svg)](https://github.com/grandnode/grandnode2/actions/workflows/codeql.yml)
</div>

<!-- TABLE OF CONTENTS -->
## Table of Contents

* [Overview](#Overview)
* [Key Features](#key-features)
* [Technical Highlights](#technical-highlights)
* [Getting Started](#getting-started)
  * [Prerequisites](#prerequisites)
  * [Installation](#installation)
  * [Building from source](#building-from-source)
  * [Running locally](#running-locally)
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

- **ASP.NET Core 10.0** - Modern, cross-platform framework
- **MongoDB 4.0+** - NoSQL database for unlimited scalability
- **Vue 3 + Bootstrap 5** - Storefront UI, bundled with Vite
- **Docker Support** - Easy deployment and containerization
- **REST API** - Comprehensive API for integrations
- **Cloud-Ready** - Optimized for cloud hosting environments
- **Real-time Processing** - Immediate updates throughout the system

<!-- GETTING STARTED -->
## Getting Started

To get a local copy up and running follow these simple steps.

### Prerequisites

| Tool | Version | Needed for |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0.100** or newer | building and running everything. The version is pinned in `global.json` with `rollForward: latestFeature`, so any 10.0.x SDK works |
| [MongoDB](https://www.mongodb.com/try/download/community) | **4.0+** | the database. A local server, a Docker container or a MongoDB Atlas cluster all work |
| [Node.js](https://nodejs.org/) + npm | **20 LTS** or newer | only when you change the storefront frontend sources. The build output is committed, so you can run the shop without Node |
| IDE | any with .NET 10 support - Visual Studio, JetBrains Rider, VS Code | optional |

Only the SDK and MongoDB are required to get the shop running - see
[Building from source](#building-from-source) for when Node.js comes into play.

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

* Open locally in an IDE

Extract the source code package downloaded from the Releases tab to a folder (or
clone the repository), and open `GrandNode.sln`. Build the whole solution - that
compiles the modules and plugins into the web project's output as well - then set
`Grand.Web` as the startup project and run it. See
[Building from source](#building-from-source) for the command line equivalent and
for the frontend build.

* Host on Linux server 

Before you start - please install, configure the nginx server, the .NET 10 SDK and MongoDB 4.0+
```bash
mkdir ~/source
cd ~/source
git clone - b x.xx https://github.com/grandnode/grandnode2.git
```
```bash
cd ~/source/grandnode
dotnet restore GrandNode.sln
```
Now it's time to rebuild all modules and plugins and publish the application. Each
module and plugin copies itself into the web project's output, so they have to be
built *before* the publish step:
```bash
for module in src/Modules/*; do dotnet build "$module" -c Release; done
for plugin in src/Plugins/*; do dotnet build "$plugin" -c Release; done
dotnet publish src/Web/Grand.Web -c Release -o /var/webapps/grandnode
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

### Building from source

#### Backend

```bash
dotnet restore GrandNode.sln
dotnet build GrandNode.sln
```

`GrandNode.sln` contains the whole application: the core libraries, the web
project, the modules under `src/Modules` (installer, migrations, REST API,
scheduled tasks) and the plugins under `src/Plugins`. Building the solution
builds them all and copies each module and plugin into
`src/Web/Grand.Web/Modules` / `Plugins`, so a plain `dotnet build` is enough.

Two things worth knowing:

* **Plugins that ship views compile those views into the plugin DLL.** After
  editing a `.cshtml` file in, for example, `src/Plugins/Theme.Modern`, rebuild
  that plugin (`dotnet build src/Plugins/Theme.Modern`) - the running site will
  not pick the change up otherwise. Razor runtime compilation covers only
  `Grand.Web`'s own views. Stop the site before rebuilding a plugin, or the
  build fails on a locked DLL.
* Building a plugin on its own is fine and is what the Docker image does; you
  only need the full solution build after changing shared code.

#### Frontend

The storefront UI (Vue 3, Bootstrap 5) lives in `src/Web/Grand.Web/vueapp` and is
the only npm project in the repository:

```bash
cd src/Web/Grand.Web/vueapp
npm install
npm run build
```

That writes into `src/Web/Grand.Web/wwwroot/bundles`:

| output | contents |
| --- | --- |
| `app.runtime.bundle.js` | Vue 3, the compatibility layer, the per-page view-models and the shared DOM behaviours |
| `libs.css` | Bootstrap, Bootstrap Icons, animate.css, Pikaday |
| `style.min.css`, `style.rtl.min.css` | the theme stylesheets from `wwwroot/theme/css`, concatenated in cascade order and minified |

**This output is committed to the repository**, which is why neither the CI
workflows nor the Dockerfile install Node - they build the .NET solution against
the bundles already in the tree. The flip side is that when you change anything
under `vueapp/src` or `wwwroot/theme/css` you have to run `npm run build` and
commit the regenerated bundles together with the source change, otherwise your
change simply will not be on the page.

Other scripts: `npm run dev` (watch build), `npm run lint` (ESLint over
`vueapp/src`), `npm run audit:prod`. More detail in
[`vueapp/README.md`](src/Web/Grand.Web/vueapp/README.md).

### Running locally

```bash
dotnet run --project src/Web/Grand.Web
```

`Grand.Web` does not reference the plugins - they install themselves into its
output directory when *they* are built. So build the solution once
(`dotnet build GrandNode.sln`) before the first run; after that you can start the
web project alone.

The Kestrel profile listens on <https://localhost:5001> and
<http://localhost:5000>; the Visual Studio IIS Express profile uses
<https://localhost:44350>.

> **Set `ASPNETCORE_ENVIRONMENT=Development`.** Both launch profiles already do.
> If you start the application without it, the static web assets manifest is not
> consulted, every file served from `_content/...` returns 404 and the admin
> panel loads with no CSS and no JavaScript at all. It looks like a broken
> install; it is only the missing environment variable.

On the first run the application redirects to `/install`, where you enter the
MongoDB connection string (for example `mongodb://localhost/grandnode`) and the
administrator account, and choose whether to load the sample data. The installer
writes the connection string to `src/Web/Grand.Web/App_Data/Settings.cfg`, which
is not tracked by git - delete that file to run the installer again against a
fresh database.

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