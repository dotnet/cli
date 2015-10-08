// Import the utility functionality.

import jobs.generation.Utilities;
import jobs.generation.InternalUtilities;

def project = 'dotnet/cli'
// Define build strings
def debugBuildString = '''build.sh debug'''
def debugTestString = '''test.sh debug'''

// Generate the builds for debug 

def linuxDebugJob = job(InternalUtilities.getFullJobName(project, 'linux_debug', false)) {
  label('linux')
  steps {
    batchFile(debugBuildString)
    batchFile(debugTestString)
  }
}

InternalUtilities.addPrivatePermissions(linuxDebugJob)
InternalUtilities.addPrivateScm(linuxDebugJob, project)
Utilities.addStandardOptions(linuxDebugJob)
Utilities.addStandardNonPRParameters(linuxDebugJob)
Utilities.addGithubPushTrigger(linuxDebugJob)


def linuxDebugPRJob = job(InternalUtilities.getFullJobName(project, 'linux_debug', true)) {
  label('linux')
  steps {
    batchFile(debugBuildString)
    batchFile(debugTestString)
  }
}

InternalUtilities.addPrivatePermissions(linuxDebugPRJob)
InternalUtilities.addPrivatePRTestSCM(linuxDebugPRJob, project)
Utilities.addStandardOptions(linuxDebugPRJob)
Utilities.addStandardPRParameters(linuxDebugPRJob, project)
Utilities.addGithubPRTrigger(linuxDebugPRJob, 'Linux Debug Build')
