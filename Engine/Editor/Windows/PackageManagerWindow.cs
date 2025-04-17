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

            EditorGUI.SameLine();

            EditorGUI.Space();

            EditorGUI.SameLine();

            var textSize = EditorGUI.GetTextSize("Remove");

            textSize.X *= 2;

            EditorGUI.SetCurrentGUICursorPosition(EditorGUI.CurrentGUICursorPosition() + new Vector2(EditorGUI.RemainingHorizontalSpace() - textSize.X, 0));

            if (PackageManager.instance.lockFile.dependencies.TryGetValue(currentPackage.name, out var state))
            {
                EditorGUI.Disabled(PackageManager.instance.lockFile.dependencies.Any(x => x.Value.dependencies.ContainsKey(currentPackage.name)), () =>
                {
                    EditorGUI.Button("Remove", "PackageManager.Description.Remove", () =>
                    {
                        PackageManager.instance.RemovePackage(currentPackage.name);
                    });
                });
            }
            else
            {
                EditorGUI.Button("Install", "PackageManager.Description.Install", () =>
                {
                    PackageManager.instance.AddPackage(currentPackage.name, currentPackage.version);
                });
            }

            EditorGUI.Label($"{currentPackage.version}");

            var sourceText = state != null ? state.source switch
            {
                PackageLockFile.Source.Local => "From local folder",
                PackageLockFile.Source.Builtin => "Builtin package",
                PackageLockFile.Source.Repository => "From package repository",
                PackageLockFile.Source.Git => $"From git at {state.url}",
                _ => "Unknown",
            } : section switch
            {
                Section.Repository => "From package repository",
                Section.BuiltIn => "Builtin package",
                _ => "Unknown",
            };

            EditorGUI.Label($"{sourceText} by {currentPackage.author}");

            EditorGUI.Label($"{currentPackage.name} ({currentPackage.license} license)");

            EditorGUI.TabBar(["Description", "Dependencies"], "PackageManager.Description.TabBar", (index) =>
            {
                switch (index)
                {
                    case 0:

                        EditorGUI.Label(currentPackage.description);

                        break;

                    case 1:

                        EditorGUI.Table("PackageManager.Description.DependenciesTable", currentPackage.dependencies.Count, 2, true, null,
                            (column) =>
                            {
                                return column switch
                                {
                                    0 => ("Name", 0),
                                    1 => ("Version", 0),
                                    _ => (null, 0),
                                };
                            },
                            (row, column) =>
                            {
                                var dependency = currentPackage.dependencies[row];

                                switch(column)
                                {
                                    case 0:

                                        EditorGUI.Label(dependency.name);

                                        break;

                                    case 1:

                                        EditorGUI.Label(dependency.version);

                                        break;
                                }
                            },
                            null);

                        break;
                }
            });
        });
    }
}
