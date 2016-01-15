Native dependencies and requirements
====================================

As any other software on the planet, the CLI toolchain and the .NET Core framework that powers it (and is powered by it) have certain native dependencies across the platforms that it supports. These dependencies are listed in this document as well as the best way to install them on your machine. 

# When do I need to worry about this?
TBD


# Ubuntu
The following dependencies are needed on Linux:

* cmake 
* llvm-3.5 
* clang-3.5 
* lldb-3.6
* lldb-3.6-dev 
* libunwind8 
* libunwind8-dev
* gettext
* libicu-dev
* liblttng-ust-dev
* libcurl4-openssl-dev
* libssl-dev
* uuid-dev

In order to get lldb-3.6 on Ubuntu 14.04, you need to add an additional package source:

```shell
echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | sudo tee /etc/apt/sources.list.d/llvm.list
wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | sudo apt-key add -
sudo apt-get update
```

Then install the packages you need:

```shell
sudo apt-get install cmake llvm-3.5 clang-3.5 lldb-3.6 lldb-3.6-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev uuid-dev
```

# Windows 

### Visual C++ Runtime for Visual Studio 2015
Install using the following installer: [VC++ RT For VS2015](https://www.microsoft.com/en-us/download/confirmation.aspx?id=48145).

### Cmake for Windows (building from source)
If you wish to build the source on your Windows box, you need to install Cmake. 

* Install [CMake](http://www.cmake.org/download) for Windows.
* Add it to the PATH environment variable.


# OS X
The following dependencies are needed on OS X. 

### OpenSSL v. 1.0.1 or later
Install using [brew](http://www.brew.sh/):

```shell
brew install openssl
brew link --force openssl
```

### ICU (International Components for Unicode)
Install using [brew](http://www.brew.sh/):

```shell
brew install icu4c
```

### Cmake
Install using [brew](http://www.brew.sh/):

```shell
brew install cmake
```
