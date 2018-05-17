// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName
def isPR = true

def platformList = [
  'CentOS7.1:x64:Debug',
  'Debian8.2:x64:Debug',
  'Fedora24:x64:Release',
  'Fedora27:x64:Debug',
  'Fedora28:x64:Release',
  'OpenSUSE42.3:x64:Release',
  'OSX:x64:Release',
  'RHEL7.2:x64:Release',
  'Ubuntu:x64:Release',
  'Ubuntu16.04:x64:Debug',
  'Ubuntu18.04:x64:Release',
  'Windows_NT:x64:Release',
  'Windows_NT:x86:Debug'
]

def static getBuildJobName(def configuration, def os, def architecture) {
    return configuration.toLowerCase() + '_' + os.toLowerCase() + '_' + architecture.toLowerCase()
}


platformList.each { platform ->
    // Calculate names
    def (os, architecture, configuration) = platform.tokenize(':')
	def osUsedForMachineAffinity = os;
    def osVersionUsedForMachineAffinity = 'latest-or-auto';

    // Calculate job name
    def jobName = getBuildJobName(configuration, os, architecture)
    def buildCommand = '';

    // Calculate the build command
    if (os == 'Windows_NT') {
        buildCommand = ".\\build.cmd -Configuration ${configuration} -Architecture ${architecture} -Targets Default"
    }
    else if (os == 'OSX') {
        buildCommand = "./build.sh --skip-prereqs --configuration ${configuration} --targets Default"
    }
    else {
        if (os == 'CentOS7.1') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            dockerFlag = "centos"
        }
        else if (os == 'Debian8.2') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            dockerFlag = "debian"
        }
        else if (os == 'Fedora24') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            osVersionUsedForMachineAffinity = 'latest-docker'
            dockerFlag = "fedora.27"
        }
        else if (os == 'Fedora27') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            osVersionUsedForMachineAffinity = 'latest-docker'
            dockerFlag = "fedora.27"
        }
        else if (os == 'Fedora28') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            osVersionUsedForMachineAffinity = 'latest-docker'
            dockerFlag = "fedora.27"
        }
        else if (os == 'OpenSUSE42.3') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            osVersionUsedForMachineAffinity = 'latest-docker'
            dockerFlag = "opensuse.42.3"
        }
        else if (os == 'RHEL7.2') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            dockerFlag = "rhel"
        }
        else if (os == 'Ubuntu') {
            osUsedForMachineAffinity = 'Ubuntu';
            dockerFlag = "ubuntu.14.04"
        }
        else if (os == 'Ubuntu16.04') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            dockerFlag = "ubuntu.16.04"
        }
        else if (os == 'Ubuntu18.04') {
            osUsedForMachineAffinity = 'Ubuntu16.04';
            osVersionUsedForMachineAffinity = 'latest-docker'
            dockerFlag = "ubuntu.18.04"
        }

        buildCommand = "./build.sh --skip-prereqs --configuration ${configuration} --docker ${dockerFlag} --targets Default"
    }

    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
        // Set the label.
        steps {
            if (os == 'Windows_NT') {
                // Batch
                batchFile(buildCommand)
            }
            else {
                // Shell
                shell(buildCommand)
            }
        }
    }

    Utilities.setMachineAffinity(newJob, osUsedForMachineAffinity, osVersionUsedForMachineAffinity)
    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
    Utilities.addXUnitDotNETResults(newJob, '**/*-testResults.xml')
    Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${architecture} ${configuration} Build")
}
