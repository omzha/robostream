workspace "robostream"
    architecture "x64"
    filename "robostream"

    configurations {"Debug", "Release"}

    project "robostream"
        location "project"
        kind "ConsoleApp"
        language "C#"
        dotnetframework "4.8"

        targetdir "bin/%{cfg.buildcfg}/"
        objdir "bin-int/%{cfg.buildcfg}/"

        files
        {
            "src/**.cs"
        }

        filter {"files:**Program.cs"}
            buildaction "None"
        filter {}

        libdirs
        {
            "C:\\Program Files (x86)\\ABB\\SDK\\PCSDK 2024"
        }

        links
        {
            "System.Windows.Forms",
            "ABB.Robotics.Controllers.PC.dll"
        }

