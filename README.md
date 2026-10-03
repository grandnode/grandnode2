<p align="center">
  <a href="https://grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme">
    <img src="https://grandnode.com/logo.png" alt="GrandNode - open-source e-commerce platform for ASP.NET Core and MongoDB">
  </a>
</p>

<h1 align="center">GrandNode - open-source e-commerce platform for .NET</h1>

<p align="center">
  <strong>Free, fast and feature-rich online store software built with ASP.NET Core 10 and MongoDB.</strong><br />
  Multi-store, multi-vendor marketplace, B2B and B2C, multi-language and multi-currency - in one self-hosted application.
</p>

<p align="center">
  <a href="https://demo.grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme"><strong>Live demo</strong></a>
  ·
  <a href="https://grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme">Website</a>
  ·
  <a href="https://docs.grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme">Documentation</a>
  ·
  <a href="https://github.com/grandnode/grandnode2/discussions">Discussions</a>
  ·
  <a href="https://grandnode.com/grandnode-themes/?utm_source=github&utm_medium=link&utm_campaign=readme">Themes</a>
  ·
  <a href="https://grandnode.com/extensions/?utm_source=github&utm_medium=link&utm_campaign=readme">Integrations & plugins</a>
  ·
  <a href="https://grandnode.com/premium-support-packages/?utm_source=github&utm_medium=link&utm_campaign=readme">Premium support</a>
</p>

<div align="center">

[![Latest release](https://img.shields.io/github/v/release/grandnode/grandnode2?sort=semver)](https://github.com/grandnode/grandnode2/releases)
[![Docker pulls](https://img.shields.io/docker/pulls/grandnode/grandnode2)](https://hub.docker.com/r/grandnode/grandnode2)
[![GitHub stars](https://img.shields.io/github/stars/grandnode/grandnode2?style=flat)](https://github.com/grandnode/grandnode2/stargazers)
[![License: GPL v3](https://img.shields.io/github/license/grandnode/grandnode2)](LICENSE)
<br />
[![Tests on Linux, macOS and Windows](https://github.com/grandnode/grandnode2/actions/workflows/aspnetcore.yml/badge.svg)](https://github.com/grandnode/grandnode2/actions/workflows/aspnetcore.yml)
[![Docker Image CI](https://github.com/grandnode/grandnode2/actions/workflows/docker-image.yml/badge.svg)](https://github.com/grandnode/grandnode2/actions/workflows/docker-image.yml)
[![CodeQL Advanced](https://github.com/grandnode/grandnode2/actions/workflows/codeql.yml/badge.svg)](https://github.com/grandnode/grandnode2/actions/workflows/codeql.yml)

</div>

## Table of Contents

* [What is GrandNode?](#what-is-grandnode)
* [Key features](#key-features)
* [Technology stack](#technology-stack)
* [Quick start](#quick-start)
* [Online demo](#online-demo)
* [Documentation](#documentation)
* [Community and support](#community-and-support)
* [Contributing](#contributing)
* [Sponsors](#sponsors)
* [License](#license)

## What is GrandNode?

GrandNode is a free, open-source **e-commerce platform written in C# on ASP.NET Core**, with **MongoDB** as its
database. It runs a single online shop just as well as a multi-store installation or a **multi-vendor
marketplace**, serves **B2B and B2C** customers from the same catalog, and exposes a **REST API** for headless
storefronts, mobile apps and integrations with ERP, PIM and CRM systems.

It is built for developers and agencies on .NET who need a shopping cart they can host anywhere - on Linux,
Windows, in Docker or Kubernetes - and extend with plugins and themes, without per-store licence fees.

## Key features

### Selling
- 🏪 **Multi-store** - run many storefronts, domains and catalogs from one installation, with a separate store owner panel
- 🤝 **Multi-vendor marketplace** - vendors manage their own products and orders in a dedicated vendor panel
- 👥 **B2B and B2C** - customer groups, tier prices, sales employees
- 🌎 **Multi-language and multi-currency** - localized content, right-to-left support, automatic exchange rates
- 🛒 **Advanced product catalog** - product attributes and combinations, bundles, grouped products, downloadable products, reservations and auctions
- 💳 **Payments** - Stripe Checkout, Braintree, cash on delivery and more through plugins
- 🚚 **Shipping** - fixed rate, by weight, pickup points
- 🧾 **Taxes** - fixed rate or by country, state and zip code

### Marketing and SEO
- 🔍 **SEO-friendly** - clean URLs, meta tags, canonical URLs, XML sitemaps and schema.org JSON-LD with price, availability, rating, shipping and return policy
- 🏷️ **Discounts and promotions** - coupon codes, discount rules, loyalty points, gift vouchers
- 🎯 **Customer segmentation** - target customer groups and tags with content, prices and discounts
- 📧 **Newsletters and messaging** - message templates, campaigns, push notifications
- 📊 **Analytics** - Google Analytics and Facebook Pixel integrations

### Look and feel
- 🎨 **Themes** - Default, Modern and Nordic Editorial storefront themes included, with per-store theme selection
- 📱 **Responsive** - Vue 3 and Bootstrap 5 storefront that works on every device
- 🧩 **Content** - CMS pages, blog, news, sliders and widget zones

## Technology stack

| Area | Technology |
| --- | --- |
| Backend | **ASP.NET Core 10**, C# |
| Database | **MongoDB 4.0+** (also Azure Cosmos DB and Amazon DocumentDB through the MongoDB API, LiteDB for small installs) |
| Storefront | **Vue 3**, Bootstrap 5, bundled with Vite |
| Admin panels | Bootstrap 5, Tabulator grids - Admin, Store owner and Vendor panels |
| API | REST backend and frontend API with OpenAPI |
| Deployment | **Docker** images on Docker Hub and GHCR, Linux, Windows, Kubernetes-ready |
| Extensibility | Plugins (payments, shipping, tax, widgets, authentication, discount rules), themes, modules |

## Quick start

### Prerequisites

| Tool | Version | Needed for |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0.100** or newer | building and running everything. The version is pinned in `global.json` with `rollForward: latestFeature`, so any 10.0.x SDK works |
| [MongoDB](https://www.mongodb.com/try/download/community) | **4.0+** | the database. A local server, a Docker container or a MongoDB Atlas cluster all work |
| [Node.js](https://nodejs.org/) + npm | **22 LTS** (22.22+) or **24 LTS** (24.15+); the admin panel tests need it, the builds alone run on 20.19+ | only when you change the storefront or admin panel frontend sources. The build output is committed, so you can run the shop without Node |
| IDE | any with .NET 10 support - Visual Studio, JetBrains Rider, VS Code | optional |

The `develop` branch is the development version and may be unstable; `main` holds the latest stable version.
Stable versions are also on the [Releases](https://github.com/grandnode/grandnode2/releases) page.

### Run with Docker

```bash
docker run -d -p 127.0.0.1:27017:27017 --name mongodb mongo
docker run -d -p 80:8080 --name grandnode2 --link mongodb:mongo -v grandnode_images:/app/wwwroot/assets/images -v grandnode_appdata:/app/App_Data grandnode/grandnode2
```

Open <http://localhost>, and the installer asks for the MongoDB connection string (`mongodb://mongo/grandnode`
with the commands above) and the administrator account.

### Run from source

```bash
git clone https://github.com/grandnode/grandnode2.git
cd grandnode2
dotnet build GrandNode.slnx
dotnet run --project src/Web/Grand.Web
```

Build the whole solution once before the first run: plugins and modules copy themselves into the web project's
output when they are built. Run with `ASPNETCORE_ENVIRONMENT=Development` (the launch profiles already set it),
otherwise the admin panel loads without its CSS and JavaScript.

More ways to install - several instances behind a load balancer, hosting on Linux with systemd - are in
[docs/installation.md](docs/installation.md). Building the frontend bundles and working on the code are in
[docs/development.md](docs/development.md).

## Online demo

* Storefront: [demo.grandnode.com](https://demo.grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme)
* Admin panel: [demo.grandnode.com/admin](https://demo.grandnode.com/admin/?utm_source=github&utm_medium=link&utm_campaign=readme) - email `admin@yourstore.com`, password `123456`

The demo is restored to its original state once per day.

## Documentation

* [Installation](docs/installation.md) - Docker, multiple instances, Linux hosting
* [Development](docs/development.md) - building the backend and frontend, running locally
* [User and developer guides](https://docs.grandnode.com/?utm_source=github&utm_medium=link&utm_campaign=readme) on docs.grandnode.com
* [Contributing guide](CONTRIBUTING.md)

## Community and support

* Questions and ideas: [GitHub Discussions](https://github.com/grandnode/grandnode2/discussions)
* Bugs and feature requests: [GitHub Issues](https://github.com/grandnode/grandnode2/issues/new/choose)
* Security vulnerabilities: see the [security policy](SECURITY.md) - please do not open public issues
* Commercial help: [premium support packages](https://grandnode.com/premium-support-packages/?utm_source=github&utm_medium=link&utm_campaign=readme)

See [SUPPORT.md](SUPPORT.md) for where each kind of question goes.

## Contributing

GrandNode is and always will be free and open-source. You can help by:

- starring the project on GitHub, so other .NET developers find it,
- reporting bugs and suggesting features in [Issues](https://github.com/grandnode/grandnode2/issues/new/choose),
- picking up a [good first issue](https://github.com/grandnode/grandnode2/issues?q=is%3Aissue%20state%3Aopen%20label%3A%22good%20first%20issue%22) and submitting a pull request - see [CONTRIBUTING.md](CONTRIBUTING.md),
- becoming a sponsor.

GrandNode has adopted the Contributor Covenant as its [Code of Conduct](CODE_OF_CONDUCT.md).

## Sponsors

Become a sponsor and get your logo on this README with a link to your site:
[become a sponsor on Open Collective](https://opencollective.com/grandnode#sponsor).

## License

GrandNode is free software distributed under the [GNU General Public License v3.0](LICENSE).
