Contributing
---

# Outline

- [Build Process](#build-process-high-level-overview)
- [Getting Started](#getting-started)
- [Editing Themes](#editing-themes)
- [Creating New Themes](#creating-new-themes)

# Build Process High level overview

I won't go into the minor details of the theme building process, however I will talk about the high level details of
what is accomplished.

All themes have a base template that they inherit from. Themes have the ability to choose their inherited parent. Each
child has the ability to override any attributes defined by the parent. This mitigates any one-off issues for themes
that are not captured by the global shared style.

# Getting Started

If this is your first time working on a Hyper plugin, then it's a good idea to complete
the [hyper plugin development setup](https://github.com/vercel/hyper/blob/canary/PLUGINS.md#plugin-development). This
should give you a good introduction as what to expect, when developing for this plugin. The `localPlugins` is going to
be `doki-theme-hyper`.

# Editing Themes

## Editing Themes Required Software

- Yarn package manager
- Node 14

## Setup

**Set up Yarn Globals**

I heavily use Node/Typescript to build all of my themes, and I have a fair amount of global tools installed.

Just run

```shell
yarn global add typescript ts-node nodemon
```

Note: if you already have these globally installed please make sure you are up to date!

```shell
yarn global upgrade typescript ts-node
```

**Get the Master Themes**

Since this theme suite expands across multiple platforms, in order to maintain consistency of the look and feel across
platforms, there is a [central theme definition repository](https://github.com/doki-theme/doki-master-theme)

This repository needs to be cloned as a directory called `masterThemes`. If you are running Linux/MacOS, you can
run `getMasterThemes.sh` located at the root of this repository. This script does exactly what is required, if you are
on Windows, have you considered Linux? Just kidding (mostly), you'll need to run this command

```shell
git clone https://github.com/doki-theme/doki-master-theme.git masterThemes
```

Your directory structure should have at least these directories, (there will probably be more, but these are the
important ones to know).

```
your-workspace/
├─ doki-theme-hyper/
│  ├─ masterThemes/
│  ├─ buildSrc/
```

Inside the `masterThemes` directory, you'll want to make sure all the dependencies are available to the build scripts.
To accomplish this, just run this command in the `masterThemes` directory.

```shell
yarn
```

### Set up build source

Navigate to the root of the `buildSource` directory and run the following command.

```shell
yarn
```

This will install all the required dependencies to run the theme build process.

You should be good to edit and add themes after that!

## Editing Themes Setup

### Running Plugin

Fire up the code watch process that builds the plugin's code and watches for typescript file changes and re-builds the
plugin. In the root of this repository run this command:

```shell
yarn watch
```

Once that is up and running, you can fire
up [hyper as described here](https://github.com/vercel/hyper/blob/canary/PLUGINS.md#running-your-plugin).

After that, you are free to make changes to the Typescript code and you can re-run the hyper instance to pick up the
changes automagically. However, you will not see changes if you make changes to the theme build process,
see [next section for more details](#theme-editing-process)

## Theme Editing Process

I have too many themes to maintain manually, so theme creation/maintenance is automated and shared common parts to
reduce overhead.

The standardized approach used by all the plugins supporting the Doki Theme suite, is that there is a `buildSrc`
directory.

Inside the `buildSrc` directory, there will be 2 directories:

- `src` - holds the code that builds the themes.
- `assets` - defines the platform specific assets needed to build the themes. This directory normally contains two child
  directories.
    - `themes` - holds the [application definitions](#application-specific-templates)
    - `templates` - if not empty, normally contains various text files that can be evaluated to replace variables with
      values. Some cases, they also contain templates for evaluating things such as look and feel, colors, and other
      things.

The `buildSrc` directory houses a `buildThemes` script that generates the application specific files necessary for apply
the Doki Theme Suite.

### Hyper Theme specifics

There is one important piece that composes a theme:

- `DokiThemeDefinition.ts` which is a generated typescript file that houses all of the information necessary to:
    - Color the application & syntax highlighting
    - Download remote content assets.

You can generate these artifacts by running this command in the `buildSrc` directory:

```shell
yarn buildThemes
```

When you are running your instance of Hyper, to see your changes to the theme, you are working on, run the `buildThemes`
command. However, if you are just making changes to the typescript code, that should just automatically be picked up and
reloaded. If not, just restart the development instance of hyper again.

# Creating New Themes

**IMPORTANT**! Do _not_ create brand new Doki-Themes using Hyper.js. New themes should be created from the original
JetBrains plugin which uses all the colors defined. There is also Doki Theme creation assistance provided by the IDE as
well.

Please follow
the [theme creation contributions in the JetBrains Plugin repository](https://github.com/doki-theme/doki-theme-jetbrains/blob/master/CONTRIBUTING.md#creating-new-themes)
for more details on how to build new themes.

## Creating Themes Required Software

- [Editing Themes required software](#editing-themes-required-software)

## Setup

- Follow the [editing themes setup](#editing-themes-setup)
- Set up the [doki-build-source](https://github.com/doki-theme/doki-build-source)
- You'll also probably want to have completed
  the [Doki Theme VS-Code](https://github.com/doki-theme/doki-theme-vscode/blob/master/CONTRIBUTING.md#creating-new-themes)
  process. As this plugin uses the sticker assets of the VS-Code plugin. So it helps to have those in place!

## Theme Creation Process

This part is mostly automated, for the most part. There is only one script you'll need to run.

### Application specific templates

Once you have a new master theme definitions merged into the default branch, it's now time to generate the application
specific templates, which allow us to control individual theme specific settings.

You'll want to edit the function used by `buildApplicationTemplate`
and `appName` [defined here](https://github.com/doki-theme/doki-master-theme/blob/596bbe7b258c65e485257a14887ee9b4e0e8b659/buildSrc/AppThemeTemplateGenerator.ts#L79)
in your `masterThemes` directory.

In the case of this plugin the `buildApplicationsTemplate` should use the `hyperTemplate` and `appName` should
be `hyper`.

We need run the `generateTemplates` script. Which will walk the master theme definitions and create the new templates in
the `<repo-root>/buildSrc/assets/themes` directory (and update existing ones). In
the `<your-workspace>/doki-theme-hyper/masterThemes` run this command:

```shell
yarn generateTemplates
```

If you added a new anime, you'll need to add
a [new group mapping](https://github.com/doki-theme/doki-build-source/blob/ee91f334588714473486a9a4b6092e10f0ce4cc1/src/GroupToNameMapping.ts#L3)
to the Doki Build source. Please see
the [handy development setup for more details on what to do](https://github.com/doki-theme/doki-build-source#doki-theme-build-source)
. You'll need to link `doki-build-source` in this plugin's build source.

For clarity, you'll have to run this command in this directory `<your-workspace>/doki-theme-hyper/buildSrc`:

```shell
yarn link doki-build-source
```

The code defined in the `buildSrc/src` directory is part of the common Doki Theme construction suite. All other plugins
work the same way, just some details change for each plugin, looking at
you [doki-theme-web](https://github.com/doki-theme/doki-theme-web). This group of code exposes a `buildThemes` node
script.

This script does all the annoying tedious stuff such as:

- Evaluating the `DokiThemeDefinitions` from the templates. See [Hyper.js Specifics](#hyper-theme-specifics) for more
  details.

[Here is an example pull request that captures all the artifacts from the development process of imported themes](https://github.com/doki-theme/doki-theme-hyper/pull/46)
.
