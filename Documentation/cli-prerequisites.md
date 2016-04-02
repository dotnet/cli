CLI native prerequisites
=========================

This document outlines the dependencies needed to run .NET Core CLI tools. Most of these dependencies are also .NET Core's general dependencies, so installing them will make sure that you can run applications written for .NET Core in general.

## Windows dependencies
On Windows, the only dependency is the VC++ Redistributable. Depending on the version of Windows you are running on, the versions are changing.

* Windows 10
    * [Visual C++ Redistributable for Visual Studio 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48145)
* Windows 7 & 8, Windows Server 2008 & 2012
    *  [Visual C++ Redistributable for Visual Studio 2012 Update 4](https://www.microsoft.com/en-us/download/confirmation.aspx?id=30679)

## Ubuntu
Ubuntu distributions require the following libraries installed:

* libc6
* libedit2
* libffi6
* libgcc1
* libicu52
* liblldb-3.6
* libllvm3.6
* liblttng-ust0
* liblzma5
* libncurses5
* libpython2.7
* libstdc++6
* libtinfo5
* libunwind8
* liburcu1
* libuuid1
* zlib1g
* libasn1-8-heimdal
* libcomerr2
* libcurl3
* libgcrypt11
* libgnutls26
* libgpg-error0
* libgssapi3-heimdal
* libgssapi-krb5-2
* libhcrypto4-heimdal
* libheimbase1-heimdal
* libheimntlm0-heimdal
* libhx509-5-heimdal
* libidn11
* libk5crypto3
* libkeyutils1
* libkrb5-26-heimdal
* libkrb5-3
* libkrb5support0
* libldap-2.4-2
* libp11-kit0
* libroken18-heimdal
* librtmp0
* libsasl2-2
* libsqlite3-0
* libssl1.0.0
* libtasn1-6
* libwind0-heimdal

## CentOS
CentOS distributions require the following libraries installed:

* deltarpm
* epel-release
* unzip
* libunwind
* gettext 
* libcurl-devel 
* openssl-devel 
* zlib 
* libicu-devel

## OS X 
OS X requires the following libraries and versions installed:

* libssl 1.1

## Installing the dependencies
Please follow the reccomended practices of each operating system in question. For Linux, we reccomend using your package manager such as `apt-get` for Ubuntu and `yum` for CentOS. For OS X and upgrading the libssl, we reccomend using [Homebrew](https://brew.sh/) 
