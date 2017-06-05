#!/bin/bash

## Constants ##
source ./config.shprops

package_dir="./${PACKAGE_NAME}-${PACKAGE_VERSION}"
debian_dir="${package_dir}/debian"

driver_binaries_path="../dotnet_driver/package_files"
driver_binaries_install_path="driver" # relative to $INSTALL_ROOT

# Used to keep state for add_install_placement function
placement_index=0

## Utility functions ##

# add_install_placement
# Summary:  Registers a file placement on the filesystem from the package
# Usage: add_install_placement {local path of file in package} {absolute path of directory to place file in}
add_install_placement(){
    if [ -z "$1"  ] || [ -z "$2"  ]; then
        echo -e "add_install_placement requires 2 parameters\n Param1: local path of file to place \n Param2: absolute path of placement on client machine"
        exit 1
    fi
    
    install_placement[${placement_index}]="${1} ${2}"
    
    placement_index=${placement_index}+1
}

# write_install_file
# Summary: Writes the contents of the "install_placement" array to the debian/install
#   This array is populated by calls to the "add_install_placement" function
# Usage: write_install_file
write_install_file(){
    rm ${debian_dir}/install

    for i in "${install_placement[@]}"
    do
        echo "${i}" >> "${debian_dir}/install"
    done
}

# add_dir_to_install
# Summary: Adds contents of a directory on the local filesystem to the package and installs them rooted at INSTALL_ROOT
#     Note: Does not install the directory passed, only its contents 
# Usage: add_dir_to_install {relative path of directory to copy} {relative path to INSTALL_ROOT to place directory tree}
add_dir_to_install(){
    if [ -z "$1" ] || [ -z "$2" ]; then
        echo -e "add_dir_to_install requires 2 parameters\n Param1: relative path to copy from \n Param2: relative path appended to INSTALL_ROOT where dir is installed"
        exit 1
    fi
    
    copy_from=$1
    src_path=$2

    rm -r ./src/${src_path}
    mkdir -p ./src/${src_path}

    #Get path to all files, relative to ${copy_from}, includes directories
    shopt -s globstar
    dir_files=( "${copy_from}/"** )
    dir_files=( "${dir_files[@]#${copy_from}}" )
    
    for dir_relfilepath in "${dir_files[@]}"
    do
        #Directories are included in the list, so check if this is a file
        if [ -f ${copy_from}${dir_relfilepath} ]; then
            local parent_dir=$(dirname $dir_relfilepath)
            local filename=$(basename $dir_relfilepath)
                
            mkdir -p ./src/${src_path}${parent_dir}
            cp "${copy_from}${dir_relfilepath}" "./src/${src_path}${parent_dir}"
            add_install_placement "${src_path}${dir_relfilepath}" "${INSTALL_ROOT}/${src_path}${parent_dir}"
            echo "Added ${filename} to install"
        fi
    done    
}

# add_dir_to_package
# Summary: Adds contents of a directory on the local filesystem to the package
#     Note: Does not copy the directory passed, only its contents. Does not install directory.
# Usage: add_dir_to_package {relative path of directory to copy} {relative path inside package to place directory contents}
add_dir_to_package(){
    if [ -z "$1" ] || [ -z "$2" ]; then
        echo "add_dir_to_install requires 2 parameters"
        echo "Param1: relative path to directory to copy contents from"
        echo "Param2: relative path inside package to place directory contents"
        exit 1
    fi
    
    copy_from=$1
    src_path=$2

    rm -r ./src/${src_path}
    mkdir -p ./src/${src_path}

    #Get path to all files, relative to ${copy_from}, includes directories
    shopt -s globstar
    dir_files=( "${copy_from}/"** )
    dir_files=( "${dir_files[@]#${copy_from}}" )
    
    for dir_relfilepath in "${dir_files[@]}"
    do
        #Directories are included in the list, so check if this is a file
        if [ -f ${copy_from}${dir_relfilepath} ]; then
            local parent_dir=$(dirname $dir_relfilepath)
            local filename=$(basename $dir_relfilepath)
                
            mkdir -p ./src/${src_path}${parent_dir}
            cp "${copy_from}${dir_relfilepath}" "./src/${src_path}${parent_dir}"
            echo "Added ${filename} to package"
        fi
    done    
}

## Build Functions ##

build_driver(){
    # TODO: Not implemented
    echo "Build not implemented"
}

build_native_toolchain(){
    # TODO: Not implemented
    echo "Build not implemented"
}

build_il_toolchain(){
    # TODO: Not Implemented
    echo "Build not implemented"
}


## Packaging Functions ##

package_driver() {
    add_dir_to_install ${driver_binaries_path} ${driver_binaries_install_path}
}

package_scripts(){
    add_dir_to_install "./scripts" "scripts"
}

package_docs(){
    # Generate Manpages based on docs.json file
    generate_manpages
}

package_samples(){
    add_dir_to_install "./samples" "samples"
    generate_sample_manifest "samples"
}

package_config(){
    cp ./config.shprops ./src/
    add_install_placement config.shprops ${INSTALL_ROOT}/config
}

package_nuget_client(){
    # Function kept here for consistency
    echo "Nuget Client currently pulled down via dnvm post install"
    echo "See postinst script in ${PACKAGE_NAME}-${PACKAGE_VERSION}/debian"
}


package_standard_libraries(){
    # Function kept here for consistency
    echo "Standard libraries currently pulled down via nuget post install"
    echo "See postinst script in ${PACKAGE_NAME}-${PACKAGE_VERSION}/debian and install_project.json"
}

package_il_debugger(){
    # TODO: Not Implemented
    echo "IL debugger packaging not implemented"
}

package_native_debugger(){
    # TODO: Not Implemented
    echo "Native debugger packaging not yet implemented"
}

package_all() {
    package_driver
    package_nuget_client
    package_il_debugger
    package_native_debugger
    package_samples
    package_docs
    package_standard_libraries
    package_scripts
    package_config

    # Other Files
    add_install_placement project.json ${INSTALL_ROOT}
    add_install_placement dotnet /usr/bin
    add_install_placement coreclr/project.json ${INSTALL_ROOT}/coreclr
}

## Generation Functions ##

generate_manpages(){
    docs_dir="./src/docs"

    # Clean the docs folder
    rm -r $docs_dir
    mkdir $docs_dir
    
    # Generate the manpages from json spec
    python ./build_tools/manpage_generator.py ../docs.json ${docs_dir}
    
    # Create Package manifest of manpages, by looking at every file in docs_dir
    shopt -s globstar
    generated_manpages=( "${docs_dir}/"* )
    generated_manpages=( "${generated_manpages[@]#./src/}" )

    rm ${debian_dir}/${PACKAGE_NAME}.manpages
    for manpage in $generated_manpages
    do
        # Only put files in manifest
        if [ -f "${debian_dir}/${manpage}" ]; then
            echo "$manpage" >> "${debian_dir}/${PACKAGE_NAME}.manpages"
        fi
    done
}

# generate_sample_manifest
# Usage: generate_sample_manifest {package relative path of sample directory}
generate_sample_manifest(){
    if [ -z "$1" ]; then
        echo "generate_sample_manifest requires a parameter."
        echo "Usage: generate_sample_manifest {package relative path of sample directory}"
    fi

    sample_manifest="${debian_dir}/${PACKAGE_NAME}.examples"
    samples_dir="${package_dir}/$1"

    shopt -s globstar
    samples=( "${samples_dir}/"* )
    samples=( "${samples[@]#${package_dir}/}" )

    rm sample_manifest
    for sample in $samples
    do
        # Only put files in manifest
        if [ -f "${package_dir}/${sample}" ]; then
            echo "$sample" >> "${sample_manifest}"
        fi
    done
}

## Debian Package Creation Functions ##
clean_or_create_build_dir(){
    rm -r ${package_dir}
    mkdir ${package_dir}

    cp -a ./package_files/. ./${package_dir}
}

create_source_tarball(){
    rm ./${PACKAGE_NAME}_${PACKAGE_VERSION}.orig.tar.gz
    tar -cvzf ${PACKAGE_NAME}_${PACKAGE_VERSION}.orig.tar.gz ./src/*
}

copy_source_to_package(){
    cp -a ./src/. ${package_dir}/
}

create_package(){
    
    create_source_tarball
    copy_source_to_package
    
    (cd ${package_dir}; debuild -us -uc)
}

clean_or_create_build_dir
package_all
write_install_file
create_package
