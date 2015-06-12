# [Save All the Tabs for Visual Studio](https://visualstudiogallery.msdn.microsoft.com/0c5642e8-f6aa-4353-830c-946a315c163d)

Quickly save and restore sets of document tabs. **Save All the Tabs** supports Visual Studio 2013 / 2015.

## Why you *need* Save All the Tabs?

Have you ever wished you could save all your opened documents before switching context to start working on something else? Or wished you could quickly switch between different distinct areas of a project; save your tabs before switching branches; save tabs for each feature/bug/PR (Pull Request) you are working on to quickly switch between them?

If you answered yes to any of those questions, then **Save All the Tabs is for you!**

## How to use Save All the Tabs?

**Save All the Tabs** adds new commands to the Visual Studio Window menu and a Saved Tabs dockable window.

![Save All the Tabs Commands](/readme-commands.png?raw=true "Save All the Tabs Commands")

### Commands

- **Save Tabs...** `Ctrl+D, Ctrl+S` - saves the current set of opened document tabs
- **Restore Tabs** menu - displays a list of the favorited (1-9) sets of document tabs
  - `Ctrl+D, 1` through `Ctrl+D, 9` - closes all the opened documents and opens the selected set of document tabs
- **Stash Tabs** `Ctrl+D, Ctrl+C` - stashes (saves to a well-known name, \<stash\>) the current set of opened documents tabs
- **Restore Stashed Tabs** `Ctrl+D, Ctrl+Z` or ``Ctrl+D, ` `` - closes all the opened documents and opens the stashed (\<stash\>) set of documents tabs
- **Saved Tabs Window** `Ctrl+D, Ctrl+W` - opens the Saved Tabs window

### Saved Tabs Window

Open the Saved Tabs window to quickly and easily manage your sets of saved document tabs. From here you can add new tabs sets, rename or delete existing tab sets. You can also double-click on a saved tab set to restore it.

![Saved Tabs Window](/readme-saved-tabs-window.png?raw=true "Saved Tabs Window")

#### Toolbar Commands

- **New Tab Set** - adds a new tab set containing the set of opened document tabs
- **Save Tabs to Selected Set** - replaces the selected tab set with the set of opened document tabs
- **Restore Tabs** - closes all the opened documents and opens the selected set of saved document tabs
- **Open Tabs** - opens the selected set of saved document tabs
- **Close Tabs** - closes any opened documents if it is contained within the selected set of saved document tabs
- **Delete** - deletes the selected tab set

#### Shortcut Keys

- `Enter` - closes all the opened documents and opens the set of saved document tabs
- `1` through `9` - assigns a favorite number to the selected tab set
- `Ctrl+UpArrow` - moves the selected tab set up
- `Ctrl+DownArrow` - moves the selected tab set down
- `Delete` - deletes the selected tab set
