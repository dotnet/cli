#!/usr/bin/env bash
# 
# Debian Packaging Script
# Currently Intended to build on ubuntu14.04

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

UNAME=$(uname)

  
if [ "$UNAME" != "Linux" ]; then
    echo "Error: Build only supported on Linux"
    exit 1
fi

REPO_ROOT=$(readlink -f $DIR/..)

OUTPUT_DIR="$REPO_ROOT/artifacts"
PACKAGE_LAYOUT_DIR="$OUTPUT_DIR/deb_intermediate"
PACKAGE_OUTPUT_DIR="$OUTPUT_DIR/packages/debian"
REPO_BINARIES_DIR="$REPO_ROOT/artifacts/ubuntu.14.04-x64/stage2"

execute(){
    create_empty_debian_layout
    copy_files_to_debian_layout
    create_debian_package
    test_debian_package
}

create_empty_debian_layout(){
    rm -rf $PACKAGE_LAYOUT_DIR
    mkdir -p $PACKAGE_LAYOUT_DIR

    mkdir "$PACKAGE_LAYOUT_DIR/\$"
    mkdir "$PACKAGE_LAYOUT_DIR/package_root"
    mkdir "$PACKAGE_LAYOUT_DIR/samples"
    mkdir "$PACKAGE_LAYOUT_DIR/docs"
}

copy_files_to_debian_layout(){
    # Copy Built Binaries
    cp -a "$REPO_BINARIES_DIR/." "$PACKAGE_LAYOUT_DIR/package_root"

    # Copy config file
    cp "$REPO_ROOT/debian_config.json" "$PACKAGE_LAYOUT_DIR"
}

create_debian_package(){
    mkdir -p $PACKAGE_OUTPUT_DIR
    
    $REPO_ROOT/package_tool/package_tool $PACKAGE_LAYOUT_DIR $PACKAGE_OUTPUT_DIR
}

test_debian_package(){
    git clone https://github.com/sstephenson/bats.git /tmp/bats
    pushd /tmp/bats
    ./install.sh /usr/local
    popd

    bats $PACKAGE_OUTPUT_DIR/test_package.bats
}

execute
