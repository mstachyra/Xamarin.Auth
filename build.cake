/*
Installing

	Windows

        Invoke-WebRequest http://cakebuild.net/download/bootstrapper/windows -OutFile build.ps1
        .\build.ps1

	Linux

        curl -Lsfo build.sh http://cakebuild.net/download/bootstrapper/linux
        chmod +x ./build.sh && ./build.sh

	OS X

        curl -Lsfo build.sh http://cakebuild.net/download/bootstrapper/osx
        chmod +x ./build.sh && ./build.sh

Running

	MacOSX 
	
		mono tools/Cake/Cake.exe --verbosity=diagnostic --target=libs
		mono tools/Cake/Cake.exe --verbosity=diagnostic --target=nuget
		
	Windows

		tools\Cake\Cake.exe --verbosity=diagnostic --target=libs
		tools\Cake\Cake.exe --verbosity=diagnostic --target=nuget
		tools\Cake\Cake.exe --verbosity=diagnostic --target=samples
*/	
#addin "Cake.Xamarin"
#addin nuget:?package=Cake.FileHelpers

var TARGET = Argument ("t", Argument ("target", Argument ("Target", "Default")));

FilePath nuget_tool_path = null;
FilePath cake_tool_path = null;

Task ("nuget-fixes")
	.Does 
	(
		() => 
		{
			/*
				2016-12-19
				Fixing nuget 3.4.4 on windows - parsing solution file
				
				2016-09
				Temporary fix for nuget bug MSBuild.exe autodetection on MacOSX and Linux

				This target will be removed in the future! 

				Executing: /Users/builder/Jenkins/workspace/Components-Generic-Build-Mac/CI/tools/Cake/../
				nuget.exe restore "/Users/builder/Jenkins/workspace/Components-Generic-Build-Mac/CI/Xamarin.Auth/source/source/Xamarin.Auth-Library.sln" -Verbosity detailed -NonInteractive
				MSBuild auto-detection: using msbuild version '4.0' from '/Library/Frameworks/Mono.framework/Versions/4.4.1/lib/mono/4.5'. 
				Use option -MSBuildVersion to force nuget to use a specific version of MSBuild.
				MSBuild P2P timeout [ms]: 120000
				System.AggregateException: One or more errors occurred. 
				---> 
				NuGet.CommandLineException: MsBuild.exe does not exist at '/Library/Frameworks/Mono.framework/Versions/4.4.1/lib/mono/4.5/msbuild.exe'.
			
				NuGet Version: 3.4.4.1321

				https://dist.nuget.org/index.html

				Xamarin CI MacOSX bot uses central cake folder
					.Contains("Components-Generic-Build-Mac/CI/tools/Cake");
			*/
			nuget_tool_path = GetToolPath ("../nuget.exe");
			cake_tool_path = GetToolPath ("./Cake.exe");

			bool runs_on_xamarin_ci_macosx_bot = false;
			string path_xamarin_ci_macosx_bot = "Components-Generic-Build-Mac/CI/tools/Cake"; 

			string nuget_location = null;
			string nuget_location_relative_from_cake_exe = null;
			
			if (cake_tool_path.ToString().Contains(path_xamarin_ci_macosx_bot))
			{
				runs_on_xamarin_ci_macosx_bot = true;
			}
			else
			{
				Information("NOT Running on Xamarin CI MacOSX bot");
			}

			if (runs_on_xamarin_ci_macosx_bot)
			{
				Information("Running on Xamarin CI MacOSX bot");
				
				nuget_location = "../../tools/nuget.2.8.6.exe";
				nuget_location_relative_from_cake_exe = "../nuget.2.8.6.exe";
				
				Information("nuget_location = {0} ", nuget_location);
			}
			else
			{
				if (IsRunningOnWindows())
				{
					// new nuget is needed for UWP!
					Information("Running on Windows");
					nuget_location = "./tools/nuget.3.5.0.exe";
					nuget_location_relative_from_cake_exe = "../nuget.3.5.0.exe";
					Information("On Mac downloading 3.5.0 to " + nuget_location);				
					DownloadFile
					(					
						@"https://dist.nuget.org/win-x86-commandline/v3.5.0/nuget.exe",
						nuget_location
					);
				}
				else
				{
					Information("Running on MacOSX (non-Windows)");
					nuget_location = "./tools/nuget.2.8.6.exe";
					Information("On Mac downloading 2.8.6 to " + nuget_location);				
					nuget_location_relative_from_cake_exe = "../nuget.2.8.6.exe";
					DownloadFile
					(
						@"https://dist.nuget.org/win-x86-commandline/v2.8.6/nuget.exe",
						nuget_location
					);
				}
			}
		
			DirectoryPath path01 = MakeAbsolute(Directory("./"));
			string path02 = System.IO.Directory.GetCurrentDirectory();
			string path03 = Environment.CurrentDirectory;
			// Cake - WorkingDirectory??
			Information("path01         = {0} ", path01);
			Information("path02         = {0} ", path02);
			Information("path03         = {0} ", path03);
			Information("cake_tool_path = {0} ", cake_tool_path);
			
			nuget_tool_path = GetToolPath (nuget_location_relative_from_cake_exe);

			Information("nuget_tool_path = {0}", nuget_tool_path);

			return;
		}
	);

RunTarget("nuget-fixes");	// fix nuget problems on MacOSX

NuGetRestoreSettings nuget_restore_settings = new NuGetRestoreSettings 
		{ 
			ToolPath = nuget_tool_path,
			Verbosity = NuGetVerbosity.Detailed
		};

Task ("clean")
	.Does 
	(
		() => 
		{	
			// note no trailing backslash
			DeleteDirectories(GetDirectories("./output"), recursive:true);
			DeleteDirectories(GetDirectories("./source/**/bin"), recursive:true);
			DeleteDirectories(GetDirectories("./source/**/obj"), recursive:true);
			DeleteDirectories(GetDirectories("./source/**/Bin"), recursive:true);
			DeleteDirectories(GetDirectories("./source/**/Obj"), recursive:true);
			DeleteDirectories(GetDirectories("./samples/**/bin"), recursive:true);
			DeleteDirectories(GetDirectories("./samples/**/obj"), recursive:true);
			DeleteDirectories(GetDirectories("./samples/**/Bin"), recursive:true);
			DeleteDirectories(GetDirectories("./samples/**/Obj"), recursive:true);
		}
	);

Task ("distclean")
	.IsDependentOn ("clean")
	.Does 
	(
		() => 
		{	
			CleanDirectories("./**/packages");
			CleanDirectories("./**/Components");
		}
	);

Task ("rebuild")
	.IsDependentOn ("distclean")
	.IsDependentOn ("build")
	;	

Task ("build")
	.IsDependentOn ("libs")
	.IsDependentOn ("samples")
	;	

Task ("package")
	.IsDependentOn ("libs")
	.IsDependentOn ("nuget")
	;	

Task ("libs")
	.IsDependentOn ("nuget-fixes")
	.IsDependentOn ("libs-macosx")	
	.IsDependentOn ("libs-windows")	
	.Does 
	(
		() => 
		{	
		}
	);

Task ("nuget-restore")
	.IsDependentOn ("nuget-fixes")
	.Does 
	(
		() => 
		{	
			//RunTarget("nuget-fixes");

			Information("libs nuget_restore_settings.ToolPath = {0}", nuget_restore_settings.ToolPath);

			NuGetRestore 
				(
					"./source/Xamarin.Auth-Library.sln",
					nuget_restore_settings
				);
			NuGetRestore 
				(
					"./source/Xamarin.Auth-Library-MacOSX-Xamarin.Studio.sln",
					nuget_restore_settings
				);
		}
	);

Task ("libs-macosx")
	.IsDependentOn ("nuget-fixes")
	.IsDependentOn ("nuget-restore")	
	.Does 
	(
		() => 
		{	
			CreateDirectory ("./output/");
			CreateDirectory ("./output/pcl/");
			CreateDirectory ("./output/android/");
			CreateDirectory ("./output/ios/");

			if ( ! IsRunningOnWindows() )
			{
				XBuild 
					(
						"./source/Xamarin.Auth-Library-MacOSX-Xamarin.Studio.sln",
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				XBuild 
					(
						"./source/Xamarin.Auth-Library-MacOSX-Xamarin.Studio.sln",
						c => 
						{
							c.SetConfiguration("Debug");
						}
					);
				//-------------------------------------------------------------------------------------
				XBuild
					(
						"./source/Xamarin.Auth.LinkSource/Xamarin.Auth.LinkSource.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				XBuild
					(
						"./source/Xamarin.Auth.LinkSource/Xamarin.Auth.LinkSource.csproj", 
						c => 
						{
							c.SetConfiguration("Debug");
						}
					);
				//-------------------------------------------------------------------------------------
				XBuild
					(
						"./source/Xamarin.Auth.Portable/Xamarin.Auth.Portable.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.Portable/**/Release/Xamarin.Auth.dll", 
						"./output/pcl/"
					);
				//-------------------------------------------------------------------------------------
				XBuild
					(
						"./source/Xamarin.Auth.XamarinAndroid/Xamarin.Auth.XamarinAndroid.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.XamarinAndroid/**/Release/Xamarin.Auth.dll", 
						"./output/android/"
					);
				//-------------------------------------------------------------------------------------
				XBuild
					(
						"./source/Xamarin.Auth.XamarinIOS/Xamarin.Auth.XamarinIOS.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.XamarinIOS/**/Release/Xamarin.Auth.dll", 
						"./output/iOS/"
					);
				//-------------------------------------------------------------------------------------


				//-------------------------------------------------------------------------------------
				XBuild
					(
						"./source/Extensions/Xamarin.Auth.Extensions.Portable/Xamarin.Auth.Extensions.Portable.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Extensions/Xamarin.Auth.Extensions.Portable/**/Release/Xamarin.Auth.Extensions.dll", 
						"./output/pcl/"
					);
				//-------------------------------------------------------------------------------------
				XBuild
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinAndroid/Xamarin.Auth.Extensions.XamarinAndroid.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinAndroid/**/Release/Xamarin.Auth.Extensions.dll", 
						"./output/android/"
					);
				//-------------------------------------------------------------------------------------
				XBuild
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinIOS/Xamarin.Auth.Extensions.XamarinIOS.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinIOS/**/Release/Xamarin.Auth.Extensions.dll", 
						"./output/ios/"
					);
				//-------------------------------------------------------------------------------------
			}

			return;
		}
	);


Task ("libs-windows")
	.IsDependentOn ("nuget-fixes")
	.IsDependentOn ("nuget-restore")
	.Does 
	(
		() => 
		{	
			CreateDirectory ("./output/");
			CreateDirectory ("./output/pcl/");
			CreateDirectory ("./output/android/");
			CreateDirectory ("./output/ios/");
			CreateDirectory ("./output/wp80/");
			CreateDirectory ("./output/wp81/");
			CreateDirectory ("./output/wpa81/");
			CreateDirectory ("./output/wpa81/Xamarin.Auth/");
			CreateDirectory ("./output/win81/");
			CreateDirectory ("./output/win81/Xamarin.Auth/");
			CreateDirectory ("./output/uap10.0/");
			CreateDirectory ("./output/uap10.0/Xamarin.Auth/");

			
			Information("libs nuget_restore_settings.ToolPath = {0}", nuget_restore_settings.ToolPath);

			Information("nuget restore {0}", "./source/Xamarin.Auth-Library.sln");
			NuGetRestore 
				(
					"./source/Xamarin.Auth-Library.sln",
					nuget_restore_settings
				);
			Information("nuget restore {0}", "./source/Xamarin.Auth-Library-MacOSX-Xamarin.Studio.sln");
			NuGetRestore 
				(
					"./source/Xamarin.Auth-Library-MacOSX-Xamarin.Studio.sln",
					nuget_restore_settings
				);


			if (IsRunningOnWindows ()) 
			{	
				MSBuild 
					(
						"./source/Xamarin.Auth-Library.sln",
						c => 
						{
							c.SetConfiguration("Release")
							.SetPlatformTarget(PlatformTarget.x86);
						}
					);
				MSBuild 
					(
						"./source/Xamarin.Auth-Library.sln",
						c => 
						{
							c.SetConfiguration("Debug")
							.SetPlatformTarget(PlatformTarget.x86);
						}
					);
				MSBuild 
					(
						"./source/Xamarin.Auth-Library-MacOSX-Xamarin.Studio.sln",
						c => 
						{
							c.SetConfiguration("Debug");
						}
					);
				MSBuild 
					(
						"./source/Xamarin.Auth-Library-MacOSX-Xamarin.Studio.sln",
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Xamarin.Auth.LinkSource/Xamarin.Auth.LinkSource.csproj", 
						c => 
						{
							c.SetConfiguration("Debug");
						}
					);
				MSBuild
					(
						"./source/Xamarin.Auth.LinkSource/Xamarin.Auth.LinkSource.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Xamarin.Auth.Portable/Xamarin.Auth.Portable.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.Portable/**/Release/Xamarin.Auth.dll", 
						"./output/pcl/"
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Xamarin.Auth.XamarinAndroid/Xamarin.Auth.XamarinAndroid.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.XamarinAndroid/**/Release/Xamarin.Auth.dll", 
						"./output/android/"
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Xamarin.Auth.XamarinIOS/Xamarin.Auth.XamarinIOS.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.XamarinIOS/**/Release/Xamarin.Auth.dll", 
						"./output/ios/"
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Xamarin.Auth.WindowsPhone8/Xamarin.Auth.WindowsPhone8.csproj", 
						c => 
						{
							c.SetConfiguration("Release")
							.SetPlatformTarget(PlatformTarget.x86);
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WindowsPhone8/**/Release/Xamarin.Auth.dll", 
						"./output/wp80/"
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Xamarin.Auth.WindowsPhone81/Xamarin.Auth.WindowsPhone81.csproj", 
						c => 
						{
							c.SetConfiguration("Release")
							.SetPlatformTarget(PlatformTarget.x86);
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WindowsPhone81/**/Release/Xamarin.Auth.dll", 
						"./output/wp81/"
					);
				//-------------------------------------------------------------------------------------
				/*
					Dependencies omitted!! 
					.
					├── Release
					│   ├── Xamarin.Auth
					│   │   ├── WebAuthenticatorPage.xaml
					│   │   ├── WebAuthenticatorPage.xbf
					│   │   └── Xamarin.Auth.xr.xml
					│   ├── Xamarin.Auth.dll
					│   ├── Xamarin.Auth.pdb
					│   └── Xamarin.Auth.pri
				*/
				MSBuild
					(
						"./source/Xamarin.Auth.WinRTWindows81/Xamarin.Auth.WinRTWindows81.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindows81/bin/Release/Xamarin.Auth.dll", 
						"./output/win81/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindows81/bin/Release/Xamarin.Auth.pdb", 
						"./output/win81/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindows81/bin/Release/Xamarin.Auth.pri", 
						"./output/win81/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindows81/bin/Release/Xamarin.Auth/Xamarin.Auth.xr.xml", 
						"./output/win81/Xamarin.Auth/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindows81/bin/Release/Xamarin.Auth/WebAuthenticatorPage.xaml", 
						"./output/win81/Xamarin.Auth/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindows81/bin/Release/Xamarin.Auth/WebAuthenticatorPage.xbf", 
						"./output/win81/Xamarin.Auth/"
					);
				//-------------------------------------------------------------------------------------
				/*
					Dependencies omitted!! 
					.
					├── Release
					│   ├── Xamarin.Auth
					│   │   ├── WebAuthenticatorPage.xaml
					│   │   ├── WebAuthenticatorPage.xbf
					│   │   └── Xamarin.Auth.xr.xml
					│   ├── Xamarin.Auth.dll
					│   ├── Xamarin.Auth.pdb
					│   └── Xamarin.Auth.pri
				*/
				MSBuild
					(
						"./source/Xamarin.Auth.WinRTWindowsPhone81/Xamarin.Auth.WinRTWindowsPhone81.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindowsPhone81/bin/Release/Xamarin.Auth.dll", 
						"./output/wpa81/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindowsPhone81/bin/Release/Xamarin.Auth.pdb", 
						"./output/wpa81/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindowsPhone81/bin/Release/Xamarin.Auth.pri", 
						"./output/wpa81/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindowsPhone81/bin/Release/Xamarin.Auth/Xamarin.Auth.xr.xml", 
						"./output/wpa81/Xamarin.Auth/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindowsPhone81/bin/Release/Xamarin.Auth/WebAuthenticatorPage.xaml", 
						"./output/wpa81/Xamarin.Auth/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.WinRTWindowsPhone81/bin/Release/Xamarin.Auth/WebAuthenticatorPage.xbf", 
						"./output/wpa81/Xamarin.Auth/"
					);
				//-------------------------------------------------------------------------------------
				/*
					Dependencies omitted!! 
					.
					├── Release
					│   ├── Xamarin.Auth
					│   │   ├── WebAuthenticatorPage.xaml
					│   │   └── Xamarin.Auth.xr.xml
					│   ├── Xamarin.Auth.dll
					│   ├── Xamarin.Auth.pdb
					│   └── Xamarin.Auth.pri
				*/
				MSBuild
					(
						"./source/Xamarin.Auth.UniversalWindowsPlatform/Xamarin.Auth.UniversalWindowsPlatform.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.UniversalWindowsPlatform/bin/Release/Xamarin.Auth.dll", 
						"./output/uap10.0/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.UniversalWindowsPlatform/bin/Release/Xamarin.Auth.pdb", 
						"./output/uap10.0/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.UniversalWindowsPlatform/bin/Release/Xamarin.Auth.pri", 
						"./output/uap10.0/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.UniversalWindowsPlatform/bin/Release/Xamarin.Auth/Xamarin.Auth.xr.xml", 
						"./output/uap10.0/Xamarin.Auth/"
					);
				CopyFiles
					(
						"./source/Xamarin.Auth.UniversalWindowsPlatform/bin/Release/Xamarin.Auth/WebAuthenticatorPage.xaml", 
						"./output/uap10.0/Xamarin.Auth/"
					);
				/*
					.net Native - Linking stuff - not needed
				CopyFiles
					(
						"./source/Xamarin.Auth.UniversalWindowsPlatform/bin/Release/Properties/Xamarin.Auth.rd.xml", 
						"./output/uap10.0/Properties/"
					);
				*/
				//-------------------------------------------------------------------------------------


				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Extensions/Xamarin.Auth.Extensions.Portable/Xamarin.Auth.Extensions.Portable.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Extensions/Xamarin.Auth.Extensions.Portable/**/Release/Xamarin.Auth.Extensions.dll", 
						"./output/pcl/"
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinAndroid/Xamarin.Auth.Extensions.XamarinAndroid.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinAndroid/**/Release/Xamarin.Auth.Extensions.dll", 
						"./output/android/"
					);
				//-------------------------------------------------------------------------------------
				MSBuild
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinIOS/Xamarin.Auth.Extensions.XamarinIOS.csproj", 
						c => 
						{
							c.SetConfiguration("Release");
						}
					);
				CopyFiles
					(
						"./source/Extensions/Xamarin.Auth.Extensions.XamarinIOS/**/Release/Xamarin.Auth.Extensions.dll", 
						"./output/ios/"
					);
				//-------------------------------------------------------------------------------------
			} 

			return;
		}
	);

string[] sample_solutions_macosx = new []
{
	"./samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Samples.TraditionalStandard-MacOSX-Xamarin.Studio.sln",
	//"./samples/bugs-triaging/component-2-nuget-migration-ANE/ANE-MacOSX-Xamarin.Studio.sln", // could not build shared project on CI
	"./samples/Traditional.Standard/references02nuget/Providers/Xamarin.Auth.Samples.TraditionalStandard.sln",
	"./samples/Traditional.Standard/references02nuget/Providers/Xamarin.Auth.Samples.TraditionalStandard-MacOSX-Xamarin.Studio.sln",	
};
string[] sample_solutions_windows = new []
{
	"samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Samples.TraditionalStandard.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Samples.TraditionalStandard-MacOSX-Xamarin.Studio.sln",
	"./samples/bugs-triaging/component-2-nuget-migration-ANE/ANE.sln",
	"./samples/Traditional.Standard/references02nuget/Providers/Xamarin.Auth.Samples.TraditionalStandard.sln", 
	"./samples/Traditional.Standard/references02nuget/Providers/Xamarin.Auth.Samples.TraditionalStandard-MacOSX-Xamarin.Studio.sln",	
	// "samples/Traditional.Standard/references01projects/Providers/old-for-backward-compatiblity/Xamarin.Auth.Sample.Android/Xamarin.Auth.Sample.Android.sln",
	// "samples/Traditional.Standard/references01projects/Providers/old-for-backward-compatiblity/Xamarin.Auth.Sample.iOS/Xamarin.Auth.Sample.iOS.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Sample.WindowsPhone8/Component.Sample.WinPhone8.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Sample.WindowsPhone81/Component.Sample.WinPhone81.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Sample.XamarinAndroid/Component.Sample.Android.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Sample.XamarinIOS/Component.Sample.IOS.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Samples.TraditionalStandard-MacOSX-Xamarin.Studio.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Samples.TraditionalStandard-MacOSX-Xamarin.Studio.xxx.sln",
	// "samples/Traditional.Standard/references01projects/Providers/Xamarin.Auth.Samples.TraditionalStandard.sln",
	// "samples/Traditional.Standard/references02nuget/old-for-backward-compatiblity/Xamarin.Auth.Sample.Android/Xamarin.Auth.Sample.Android.sln",
	// "samples/Traditional.Standard/references02nuget/old-for-backward-compatiblity/Xamarin.Auth.Sample.iOS/Xamarin.Auth.Sample.iOS.sln",
	// "samples/Traditional.Standard/references02nuget/Xamarin.Auth.Sample.WindowsPhone8/Component.Sample.WinPhone8.sln",
	// "samples/Traditional.Standard/references02nuget/Xamarin.Auth.Sample.WindowsPhone81/Component.Sample.WinPhone81.sln",
	// "samples/Traditional.Standard/references02nuget/Xamarin.Auth.Sample.XamarinAndroid/Component.Sample.Android.sln",
	// "samples/Traditional.Standard/references02nuget/Xamarin.Auth.Sample.XamarinIOS/Component.Sample.IOS.sln",
	// "samples/Traditional.Standard/references02nuget/Xamarin.Auth.Samples.TraditionalStandard-MacOSX-Xamarin.Studio.sln",
	// "samples/Traditional.Standard/references02nuget/Xamarin.Auth.Samples.TraditionalStandard.sln",
	// "samples/Traditional.Standard/WindowsPhoneCrashMissingMethod-GetUI/WP8/Demo.sln",
	// "samples/Traditional.Standard/WindowsPhoneCrashMissingMethod-GetUI/WP8-XA/Demo.sln",
	// "samples/Xamarin.Forms/references01project/Evolve16Labs/04-Securing Local Data/Diary.sln",
	// "samples/Xamarin.Forms/references01project/Evolve16Labs/05-OAuth/ComicBook.sln",
	// "samples/Xamarin.Forms/references01project/Providers/XamarinAuth.XamarinForms.sln",
	// "samples/Xamarin.Forms/references02nuget/04-Securing Local Data/Diary.sln",
};

string[] sample_solutions = 
			sample_solutions_macosx
			.Concat(sample_solutions_windows)  // comment out this line if in need
			.ToArray()
			;

string[] build_configurations =  new []
{
	"Debug",
	"Release",
};

Task ("samples-nuget-restore")
	.Does 
	(
		() => 
		{
			foreach (string sample_solution in sample_solutions)
			{
				NuGetRestore(sample_solution, nuget_restore_settings); 
			}
			return;
		}
	);

Task ("samples")
	.Does 
	(
		() => 
		{
			if ( IsRunningOnWindows() )
			{
				RunTarget ("samples-windows");
			}
			RunTarget ("samples-macosx");
		}
	);

Task ("samples-macosx")
	.IsDependentOn ("samples-nuget-restore")
	.IsDependentOn ("libs")
	.Does 
	(
		() => 
		{
			foreach (string sample_solution in sample_solutions_macosx)
			{
				foreach (string configuration in build_configurations)
				{
					if ( IsRunningOnWindows() )
					{
						MSBuild
							(
								sample_solution, 
								c => 
								{
									c.SetConfiguration(configuration);
								}
							);						
					}
					else
					{
						XBuild
							(
								sample_solution, 
								c => 
								{
									c.SetConfiguration(configuration);
								}
							);						
					}
				}
			}

			return;
		}
	);

Task ("samples-windows")
	.IsDependentOn ("samples-nuget-restore")
	.IsDependentOn ("libs")
	.Does 
	(
		() => 
		{
			foreach (string sample_solution in sample_solutions_windows)
			{
				foreach (string configuration in build_configurations)
				{
					if ( IsRunningOnWindows() )
					{
					}
					else
					{
						XBuild
							(
								sample_solution, 
								c => 
								{
									c.SetConfiguration(configuration);
								}
							);						
					}
				}
			}

			return;
		}
	);
	
Task ("nuget")
	.IsDependentOn ("libs")
	.Does 
	(
		() => 
		{
			if 
			(
				! FileExists("./output/wp80/Xamarin.Auth.dll")
			)
			{
				string msg =
				"Missing dll artifacts"
				+ System.Environment.NewLine +
				"Please, build on Windows first!";

				throw new System.ArgumentNullException(msg);
			}
			NuGetPack 
				(
					"./nuget/Xamarin.Auth.nuspec", 
					new NuGetPackSettings 
					{ 
						Verbosity = NuGetVerbosity.Detailed,
						OutputDirectory = "./output/",        
						BasePath = "./",
						ToolPath = nuget_tool_path
					}
				);                
			NuGetPack 
				(
					"./nuget/Xamarin.Auth.Extensions.nuspec", 
					new NuGetPackSettings 
					{ 
						Verbosity = NuGetVerbosity.Detailed,
						OutputDirectory = "./output/",        
						BasePath = "./",
						ToolPath = nuget_tool_path
					}
				);                
		}
	);

Task ("externals")
	.Does 
	(
		() => 
		{
			return;
		}
	);

Task ("component")
	.IsDependentOn ("nuget")
	.IsDependentOn ("samples")
	.Does 
	(
		() => 
		{
			var COMPONENT_VERSION = "1.3.1.1";
			var yamls = GetFiles ("./**/component.yaml");

			foreach (var yaml in yamls) 
			{
				Information("yaml = " + yaml);
				var contents = FileReadText (yaml).Replace ("$version$", COMPONENT_VERSION);
				
				var fixedFile = yaml.GetDirectory ().CombineWithFilePath ("component.yaml");
				FileWriteText (fixedFile, contents);
				
				PackageComponent 
					(
						fixedFile.GetDirectory (), 
						new XamarinComponentSettings ()
					);
			}

			if (!DirectoryExists ("./output"))
			{
				CreateDirectory ("./output");
			}

			CopyFiles ("./component/**/*.xam", "./output");		
		}
	);
FilePath GetToolPath (FilePath toolPath)
{
    var appRoot = Context.Environment.GetApplicationRoot ();
     var appRootExe = appRoot.CombineWithFilePath (toolPath);
     if (FileExists (appRootExe))
	 {
         return appRootExe;
	 }
    throw new FileNotFoundException ("Unable to find tool: " + appRootExe); 
}


//=================================================================================================
// Put those 2 CI targets at the end of the file after all targets
// If those targets are before 1st RunTarget() call following error occusrs on 
//		*	MacOSX under Mono
//		*	Windows
// 
//	Task 'ci-osx' is dependent on task 'libs' which do not exist.
//
// Xamarin CI - Jenkins job targets
Task ("ci-osx")
    .IsDependentOn ("libs")
    .IsDependentOn ("nuget")
    //.IsDependentOn ("samples")
	;
Task ("ci-windows")
    .IsDependentOn ("libs")
    .IsDependentOn ("nuget")
    //.IsDependentOn ("samples")
	;	
//=================================================================================================


RunTarget (TARGET);