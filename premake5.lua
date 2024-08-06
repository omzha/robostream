workspace "robostream"
    architecture "x64"
    filename "robostream"

    configurations {"Debug", "Release"}

    filter {"configurations:*Debug*"}
        defines {"DEBUG"}
        optimize "Off"
    filter {}

    filter {"configurations:*Release*"}
        optimize "On"
        warnings "Off"
    filter {}

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

        libdirs
        {
            "C:\\Program Files (x86)\\ABB\\SDK\\PCSDK 2024"
        }

        links
        {
            "System",
            "System.Windows.Forms",
            "System.Text.RegularExpressions",
            "ABB.Robotics.Controllers.PC.dll"
        }

