#!/usr/bin/python
"""
resolve_nuget_assets

Author: Bryan Thornbury (brthor@microsoft.com)

Summary: Parses a project.lock.json file to obtain paths to assets defined therein

"""


import sys
import os
import json

# Constants

runtime_asset_type = "runtime"
compile_asset_type = "compile"
native_asset_type = "native"
resource_asset_type = "resource"

IS_DEBUG = False

def _get_parser():	
	import optparse
	
	parser = optparse.OptionParser()
	
	parser.add_option("-f", "--framework", dest="framework",
		help="target framework moniker to resolve in the project.lock.json", metavar="MONIKER")
		
	parser.add_option("-r", "--runtime", dest="runtime",
		help="target runtime to resolve in the project.lock.json", metavar="RUNTIME_ID")
		
	parser.add_option("-p", "--packages", dest="package_cache",
		help="absolute path to the package cache, will be prepended to output", metavar="RUNTIME_ID")
	
	parser.add_option("-l","--lib", dest="runtime_assets", default=False, action="store_true",
		help="flag to resolve runtime assets")
	
	parser.add_option("-c","--compile", dest="compile_assets", default=False, action="store_true",
		help="flag to resolve compile assets")
		
	parser.add_option("-s","--resource", dest="resource_assets", default=False, action="store_true",
		help="flag to resolve native assets")
	
	parser.add_option("-n","--native", dest="native_assets", default=False, action="store_true",
		help="flag to resolve resource assets")
		
	parser.add_option("-d","--debug", dest="debug", default=False, action="store_true",
		help="flag to enable debug output")

	return parser

def _print_help():
	parser = _get_parser()
	parser.print_help()

def _parse_args():
	parser = _get_parser()	
	return parser.parse_args()
	
def _check_required_args(options, args):
	
	if not options.framework:
		print "Error: Framework not given"
		return False
	
	if not options.runtime:
		print "Error: Runtime not given"
		return False
	
	asset_types = _get_asset_types_from_options(options)
	
	if len(asset_types) == 0:
		print "Error: at Least one asset flag must be specified."
		return False

	return True
	
def _validate_args(options, args):
	if not _check_required_args(options, args):
		return False
		
	project_lock_json_path = _get_path_argument_or_default(args)
	
	if not os.path.isfile(project_lock_json_path):
		print "Error:", project_lock_json_path, "is an invalid path"
		return False
			
	return True
		
def _get_path_argument_or_default(args):
	if len(args) < 1:
		return os.path.join(os.getcwd(), "project.lock.json")
	else:
		path = args[0]
		return path
		
def _get_asset_types_from_options(options):
	asset_types = []
	
	if options.runtime_assets == True:
		asset_types.append(runtime_asset_type)

	if options.compile_assets == True:
		asset_types.append(compile_asset_type)
	if options.native_assets == True:
		asset_types.append(native_asset_type)
	if options.resource_assets == True:
		asset_types.append(resource_asset_type)
		
	return asset_types
		
def _get_target(json_data, framework, runtime):
	all_targets = json_data.get("targets", None)
	
	if all_targets == None:
		raise Exception("Improperly formatted project.lock.json file.")
	else:
		target_key = framework + "/" + runtime
		target = all_targets.get(target_key, None)
		
		if target == None:
			raise Exception("Could not find target: " + target_key)
		else:
			return target

def _output_assets(assets):
	for asset in assets:
		print asset
	
def resolve_assets(asset_types, project_lock_json_path, framework, runtime, package_cache=None):
	
	with open(project_lock_json_path) as json_file:
		json_data = json.load(json_file)
		
		target = _get_target(json_data, framework, runtime)
		
		local_asset_paths = []
		
		for package_name in target:
			package_json = target[package_name]
			
			for asset_type in asset_types:
				package_asset = package_json.get(asset_type, None)
				
				if package_asset != None:
					valid_package_assets = filter(lambda x: not x.endswith("_._"), package_asset.keys())
					local_asset_paths.extend(map(lambda path: os.path.join(package_name, path),valid_package_assets))
		
		# If the package_cache isn't specified, return local paths
		if package_cache is None:
			return local_asset_paths
			
		#Otherwise prepend the package_cache to each local path
		else:
			return map(lambda local_path: os.path.join(package_cache, local_path), local_asset_paths)
			

def execute_command_line():
	global IS_DEBUG
	
	options, args = _parse_args()
	
	if not _validate_args(options,args):
		_print_help()
		sys.exit(1)
		
	project_lock_json_path = _get_path_argument_or_default(args)
		
	if options.debug:
		IS_DEBUG=True
		
	target_framework = options.framework
	target_runtime = options.runtime
	package_cache = options.package_cache
	
	asset_types = _get_asset_types_from_options(options)
	
	try:
		assets = resolve_assets(asset_types, project_lock_json_path, target_framework, target_runtime, package_cache = package_cache)
		_output_assets(assets)
	except Exception as exc:
		print exc
		
		if IS_DEBUG:
			import traceback
			traceback.print_exc()
			
		sys.exit(1)
		
	sys.exit(0)
		
if __name__ == "__main__":
	execute_command_line()

		
