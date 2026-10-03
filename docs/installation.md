# Installing GrandNode

GrandNode can be installed in a few different ways. The `develop` branch is the development version of
GrandNode and may be unstable. The `main` branch contains the latest stable version. You can also download a
specific stable version from the [Releases](https://github.com/grandnode/grandnode2/releases) page or switch
to a release branch.

* [Docker](#docker)
* [Running more than one instance](#running-more-than-one-instance)
* [Open locally in an IDE](#open-locally-in-an-ide)
* [Host on a Linux server](#host-on-a-linux-server)

For the tools and versions you need, see [Prerequisites](../README.md#prerequisites).

## Docker

```bash
docker run -d -p 127.0.0.1:27017:27017 --name mongodb mongo
docker run -d -p 80:8080 --name grandnode2 --link mongodb:mongo -v grandnode_images:/app/wwwroot/assets/images -v grandnode_appdata:/app/App_Data grandnode/grandnode2
```

To run a specific stable version, pull its tag, where `x.xx` is a GrandNode release number:

```bash
docker pull grandnode/grandnode2:x.xx
```

## Running more than one instance

Files GrandNode writes at runtime (sitemap XML files, custom CSS/JS, uploaded images, thumbnails, the push
service worker) live in `wwwroot` by default, so with several instances each one only sees its own. Point
`Application:MediaPath` at a volume shared by all instances (ReadWriteMany in Kubernetes: Azure Files, EFS, NFS):

```bash
docker run -d -p 80:8080 --name grandnode2 --link mongodb:mongo \
  -e Application__MediaPath=/app/media -v grandnode_media:/app/media \
  -v grandnode_appdata:/app/App_Data grandnode/grandnode2
```

Files that ship with the build stay in `wwwroot` and are used until the shop overwrites them. When switching an
existing installation, **move** (not copy) `wwwroot/assets/custom`, `wwwroot/assets/images/uploaded`,
`wwwroot/sitemap*.xml`, `wwwroot/firebase-messaging-sw.js` and, with pictures stored on disk, the
`wwwroot/assets/images/*_0.*` originals into the same paths under the media path, then stop mounting the old
`wwwroot/assets/images` volume. With the `Directory` setting configured, both sides carry it as a prefix: move
`wwwroot/{Directory}/assets/custom` to `{MediaPath}/{Directory}/assets/custom`, and so on. Without the move, each
instance serves its own copy of the sitemap until the sitemap task runs again, and of the push service worker until
the push notification settings are saved again. A file left behind in `wwwroot` keeps being served after it is
deleted in the admin, because the shop only deletes on the media path. Thumbnails are regenerated on the media path.

## Open locally in an IDE

Extract the source code package downloaded from the Releases tab to a folder (or clone the repository), and open
`GrandNode.slnx`. Build the whole solution - that compiles the modules and plugins into the web project's output
as well - then set `Grand.Web` as the startup project and run it. See [Development](development.md) for the
command line equivalent and for the frontend build.

## Host on a Linux server

Before you start, install and configure the nginx server, the .NET 10 SDK and MongoDB 4.0+.

```bash
mkdir ~/source
cd ~/source
git clone -b x.xx https://github.com/grandnode/grandnode2.git
```

```bash
cd ~/source/grandnode2
dotnet restore GrandNode.slnx
```

Now rebuild all modules and plugins and publish the application. Each module and plugin copies itself into the
web project's output, so they have to be built *before* the publish step:

```bash
for module in src/Modules/*; do dotnet build "$module" -c Release; done
for plugin in src/Plugins/*; do dotnet build "$plugin" -c Release; done
dotnet publish src/Web/Grand.Web -c Release -o /var/webapps/grandnode
```

Optional: create the service file, to automatically restart your application.

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

Enable the service and start GrandNode:

```bash
sudo systemctl enable grandnode.service
sudo systemctl start grandnode.service
```

## First run

On the first run the application redirects to `/install`, where you enter the MongoDB connection string (for
example `mongodb://localhost/grandnode`) and the administrator account, and choose whether to load the sample
data. The installer writes the connection string to `App_Data/Settings.cfg` - delete that file to run the
installer again against a fresh database.
