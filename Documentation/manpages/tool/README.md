# Manpage update tool

Utility to update dotnet-cli documentation from https://github.com/dotnet/docs.

## Prerequisites

* `python` in PATH
* `pandocfilters` package installed

### Unix specific
* `pandoc` installed
* `unzip`
* `wget` or `curl`

## Usage

```sh
./update-man-pages.sh
```

## Ubuntu example (from scratch)

```sh
sudo apt install -y pandoc python python-pip wget unzip < /dev/null
sudo pip install pandocfilters

git clone https://github.com/dotnet/cli
./cli/Documentation/manpage/tool/update-man-pages.sh
```

The version of pandoc is printed in first line as code comment in `.1` (manpage) files.
If your version of pandoc is older than (or even equal to) the last modifed version,
please install the latest stable version of pandoc from https://github.com/jgm/pandoc/releases (deb file for Ubuntu):

```sh
wget -q https://github.com/jgm/pandoc/releases/download/$pandocVersion/$pandocVersionedName.deb > /dev/null
# or
# curl -sSLO https://github.com/jgm/pandoc/releases/download/$pandocVersion/$pandocVersionedName.deb > /dev/null
sudo dpkg -i $pandocVersionedName.deb
rm $pandocVersionedName.deb*
```

after that update manpages by calling `update-man-pages.sh` script.

## PowerShell example

```posh
# python 2.7.9+ has built-in pip (https://stackoverflow.com/a/12476379)
python -m pip install pandocfilters

git clone https://github.com/dotnet/cli
./cli/Documentation/manpage/tool/update-man-pages.ps1
```
