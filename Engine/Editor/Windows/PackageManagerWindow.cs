using System.Linq;
using System.Numerics;

namespace Staple.Editor;

internal class PackageManagerWindow : EditorWindow
{
    private enum Section
    {
        Project,
        Repository,
        BuiltIn,
    }

    public string basePath;

    private Section section = Section.Project;
    private Package currentPackage;

    public PackageManagerWindow()
    {
        title = "Package Manager";

        windowFlags = EditorWindowFlags.Resizable;
    }

    public override void OnGUI()
    {
        EditorGUI.MenuBar(() =>
        {
            EditorGUI.Menu("Add", "PackageManager.Add", () =>
            {
                EditorGUI.MenuItem("From Folder", "PackageManager.AddFolder", () =>
                {
                });

                EditorGUI.MenuItem("From Git", "PackageManager.AddGit", () =>
                {
                });
            });
        });

        var width = EditorGUI.RemainingHorizontalSpace();

        EditorGUI.WindowFrame("PackageManager.Sections", new Vector2(width / 4, 0), () =>
        {
            EditorGUI.TreeNode("In Project", "PackageManager.Sections.Project", true, null, () =>
            {
                section = Section.Project;
            });

            EditorGUI.TreeNode("Package Repository", "PackageManager.Sections.Repository", true, null, () =>
            {
                section = Section.Repository;
            });

            EditorGUI.TreeNode("Built-in", "PackageManager.Sections.BuiltIn", true, null, () =>
            {
                section = Section.BuiltIn;
            });
        });

        EditorGUI.SameLine();

        EditorGUI.WindowFrame("PackageManager.List", new Vector2(width / 4, 0), () =>
        {
            switch(section)
            {
                case Section.Project:

                    EditorGUI.Table("PackageManager.List.Project.Content", PackageManager.instance.projectPackages.Count, 2, true, null, 
                        (column) =>
                        {
                            return column switch
                            {
                                0 => ("Name", 0),
                                1 => ("Version", 0),
                                _ => default,
                            };
                        },
                        (row, column) =>
                        {
                            var key = PackageManager.instance.projectPackages.Keys.Skip(row).FirstOrDefault();

                            if(PackageManager.instance.projectPackages.TryGetValue(key, out var pair))
                            {
                                switch(column)
                                {
                                    case 0: //Name

                                        EditorGUI.Label(pair.Item2.displayName);

                                        break;

                                    case 1: //Version

                                        EditorGUI.Label(pair.Item2.version);

                                        break;
                                }
                            }
                        },
                        (row) =>
                        {
                            var key = PackageManager.instance.projectPackages.Keys.Skip(row).FirstOrDefault();

                            currentPackage = PackageManager.instance.projectPackages.TryGetValue(key, out var pair) ? pair.Item2 : null;
                        });

                    break;

                case Section.BuiltIn:

                    EditorGUI.Table("PackageManager.List.Builtin.Content", PackageManager.instance.builtinPackages.Count, 3, true, null,
                        (column) =>
                        {
                            return column switch
                            {
                                0 => ("Name", 0),
                                1 => ("Version", 60),
                                2 => ("Installed", 80),
                                _ => default,
                            };
                        },
                        (row, column) =>
                        {
                            var key = PackageManager.instance.builtinPackages.Keys.Skip(row).FirstOrDefault();

                            if (PackageManager.instance.builtinPackages.TryGetValue(key, out var pair))
                            {
                                switch (column)
                                {
                                    case 0: //Name

                                        EditorGUI.Label(pair.Item2.displayName);

                                        break;

                                    case 1: //Version

                                        EditorGUI.Label(pair.Item2.version);

                                        break;

                                    case 2: //Installed

                                        {
                                            var installed = PackageManager.instance.lockFile.dependencies.TryGetValue(key, out var state);

                                            if(installed)
                                            {
                                                installed = state.version == pair.Item2.version;
                                            }

                                            EditorGUI.Disabled(true, () =>
                                            {
                                                EditorGUI.Toggle("", $"PackageManager.List.Builtin.Installed{row}", installed);
                                            });
                                        }

                                        break;
                                }
                            }
                        },
                        (row) =>
                        {
                            var key = PackageManager.instance.builtinPackages.Keys.Skip(row).FirstOrDefault();

                            currentPackage = PackageManager.instance.builtinPackages.TryGetValue(key, out var pair) ? pair.Item2 : null;
                        });

                    break;
            }
        });

        EditorGUI.SameLine();

        EditorGUI.WindowFrame("PackageManager.Description", new Vector2(width / 2, 0), () =>
        {
            if(currentPackage == null)
            {
                return;
            }

            EditorGUI.HeaderLabel(currentPackage.displayName);
        });
    }
}
