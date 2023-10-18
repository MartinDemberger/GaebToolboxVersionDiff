# GaebToolboxVersionDiff

This tool can be used to compare the .Net Framework and .Net Core version of GAEB Toolbox.

## How to get started?

You have to obtain a version of the GAEB Toolbox from https://gaebtoolbox.de/gaeb-toolbox/
This must be copied the directory `GaebToolbox`.
`GAEB_Toolbox_33.dll` and all needed files must be placed inside the directory `GaebToolbox/core31`.
`GaebToolBoxV330.dll` and all needed files must be placed inside the directory `GaebToolbox/x64`.

Now you copy all GAEB files to test in the folder `InputFiles`.

Open the solution `GaebToolBoxVersionDiff.sln`, compile all projects and start `MasterApp`.

## How does it work?

Each file in `InputFiles` is converted to GAEB-90, GAEB-2000 and GAEB-XML by both toolbox versions.
The converted files are compared and if they are different or a conersion doesn't succeed the error is printed