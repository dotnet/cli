#!/bin/bash

#Ensure running with superuser priveledges
current_user=$(whoami)
if [ $current_user != "root" ]; then
	echo "testpackage.sh requires superuser privileges to run"
	exit 1
fi

#Test Utility Functions
test_build_package(){
	./build.sh

	if [ "$?" != "0" ]; then
		echo "Package build failed"
		return 1
	fi
	return 0
}

test_package_installed(){
	$(dpkg -s "dotnet")
	return $?
}

echo_red(){
	echo -e "\e[31m$1\e[0m"
}

echo_green(){
	echo -e "\e[32m$1\e[0m"
}

echo_yellow(){
	echo -e "\e[33m$1\e[0m"
}

#Remove the package
remove_package(){
	apt-get remove -y dotnet
}

install_package() {
	if [ -s "./dotnet_1.0-1_amd64.deb" ]; then
		#Package Exists
		dpkg -i ./dotnet_1.0-1_amd64.deb
		return $?
	else
		#Package does not exist
		echo "Cannot install Package, it does not exist."
		return 1
	fi

}

#Test Complete Removal
test_complete_removal(){
	$(test_package_installed)

	if [ "$?" == 0 ]; then
		echo "Package is still installed"
		return 1
	elif [ -d "/usr/share/dotnet" ]; then
		echo "/usr/share/dotnet still exists"
		return 1
	elif [ -s "/usr/bin/dotnet" ]; then
		echo "/usr/bin/dotnet still exists"
		return 1
	fi
	return 0
}

# Compare Output to Checked-in LKG output for testdocs.json
test_manpage_generator(){

	python ./build_tools/manpage_generator.py ./build_tools/tests/testdocs.json ./build_tools/tests

	# Output is file "tool1.1"
	# LKG file is "lkgtestman.1"

	difference=$(diff ./build_tools/tests/tool1.1 ./build_tools/tests/lkgtestman.1)

	if [ -z "$difference" ]; then
		return 0
	else
		echo "Bad Manpage Generation"
		echo $difference
		return 1
	fi
	
}


#Test Help Message (Baseline Test)
test_dotnet_exists(){
	output=$(dotnet --help)

	if [ "$?" != "0" ]; then
		echo "dotnet help failed:"
		echo $output
		return 1
	fi
	return 0
}

test_coreclr_exists(){
	coreclr_root="/usr/share/dotnet/coreclr"

	if [ -f "${coreclr_root}/CoreRun" ]; then
		return 0
	else
		echo "CoreRun does not exist in ${corecor_root}"
		return 1
	fi
}

test_dotnet_builtin_commands(){
	output=$(cd ./builtins; dotnet-commands )

	if [[ "$output" == "commands" ]]; then
		return 0
	else
		echo $output
		return 1
	fi
}

run_test_function() {
	if [ -z "$1" ]; then
		echo "run_test_function requires a test function name as the first parameter"
		exit 1
	fi
	
	test_command=$1
	echo_yellow "Running test: $test_command ..."
	output=$( $test_command )

	if [ "$?" != 0 ]; then
		echo_red "$test_command failed"
		exit 1
	else
		echo_green "$test_command succeeded"
		return 0
	fi
}

run_tests(){
	#If the package is already installed remove it, and test that
	$( test_package_installed )

	if [ "$?" == 0 ]; then
		echo_yellow "dotnet package installed, removing first"
		$(remove_package)
		test_complete_removal

		if [ "$?" != 0]; then
			echo_red "Complete removal failed"
			echo_red "Fix removal before re-running tests"
			exit 1
		fi
	fi
	
	echo_yellow "Running All Tests..."

	# Run all tests
	run_test_function test_manpage_generator
	run_test_function test_build_package
	run_test_function install_package
	run_test_function test_dotnet_exists
	run_test_function test_coreclr_exists
	
	# Test package removal
	$(remove_package)
	run_test_function test_complete_removal
		
}

# Allow for testing specific pieces only
if [ "$1" == "build" ]; then
	run_test_function test_build_package
elif [ "$1" == "install" ]; then
	run_test_function install_package
elif [ "$1" == "builtin" ]; then
	run_test_function test_dotnet_builtin_commands
else
	run_tests
fi

