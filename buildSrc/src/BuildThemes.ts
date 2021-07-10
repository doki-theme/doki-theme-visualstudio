import {
  BaseAppDokiThemeDefinition,
  constructNamedColorTemplate,
  DokiThemeDefinitions,
  evaluateTemplates,
  MasterDokiThemeDefinition,
  resolvePaths,
  StringDictionary,
} from "doki-build-source";
import omit from 'lodash/omit';
import fs from "fs";
import path from "path";

type AppDokiThemeDefinition = BaseAppDokiThemeDefinition;

const {
  repoDirectory,
  masterThemeDefinitionDirectoryPath,
  appTemplatesDirectoryPath,
} = resolvePaths(__dirname);

// todo: dis
type DokiThemeVisualStudio = {
  [k: string]: any;
};


function buildTemplateVariables(
  dokiThemeDefinition: MasterDokiThemeDefinition,
  masterTemplateDefinitions: DokiThemeDefinitions,
  dokiThemeAppDefinition: AppDokiThemeDefinition,
): DokiThemeVisualStudio {
  const namedColors: StringDictionary<string> = constructNamedColorTemplate(
    dokiThemeDefinition,
    masterTemplateDefinitions
  );
  const colorsOverride =
    dokiThemeAppDefinition.overrides.theme?.colors || {};
  const cleanedColors = Object.entries(namedColors)
    .reduce((accum, [colorName, colorValue]) => ({
      ...accum,
      [colorName]: colorValue,
    }), {});
  return {
    ...cleanedColors,
    ...colorsOverride,
  };
}

function createDokiTheme(
  masterThemeDefinitionPath: string,
  masterThemeDefinition: MasterDokiThemeDefinition,
  appTemplateDefinitions: DokiThemeDefinitions,
  appThemeDefinition: AppDokiThemeDefinition,
  masterTemplateDefinitions: DokiThemeDefinitions,
) {
  try {
    return {
      path: masterThemeDefinitionPath,
      definition: masterThemeDefinition,
      stickers: getStickers(masterThemeDefinition, masterThemeDefinitionPath),
      templateVariables: buildTemplateVariables(
        masterThemeDefinition,
        masterTemplateDefinitions,
        appThemeDefinition,
      ),
      theme: {},
      appThemeDefinition: appThemeDefinition,
    };
  } catch (e) {
    throw new Error(
      `Unable to build ${masterThemeDefinition.name}'s theme for reasons ${e}`
    );
  }
}

function resolveStickerPath(themeDefinitionPath: string, sticker: string) {
  const stickerPath = path.resolve(
    path.resolve(themeDefinitionPath, ".."),
    sticker
  );
  return stickerPath.substr(
    masterThemeDefinitionDirectoryPath.length + "/definitions".length
  ).replace(/\\/g, '/');
}

const getStickers = (
  dokiDefinition: MasterDokiThemeDefinition,
  themePath: string
) => {
  const secondary =
    dokiDefinition.stickers.secondary || dokiDefinition.stickers.normal;
  return {
    defaultSticker: {
      path: resolveStickerPath(themePath, dokiDefinition.stickers.default),
      name: dokiDefinition.stickers.default,
    },
    ...(secondary
      ? {
        secondary: {
          path: resolveStickerPath(themePath, secondary),
          name: secondary,
        },
      }
      : {}),
  };
};

console.log("Preparing to generate themes.");
const solutionsDirectory = path.resolve(repoDirectory, "doki-theme-visualstudio");

const generatedThemesDirectory = path.resolve(solutionsDirectory, 'Themes', 'generated')

if (!fs.existsSync(generatedThemesDirectory)) {
  fs.mkdirSync(generatedThemesDirectory, {recursive: true})
}

evaluateTemplates(
  {
    appName: 'visualstudio',
    currentWorkingDirectory: __dirname,
  },
  createDokiTheme
)
  .then((dokiThemes) => {

    const darkTemplate = fs.readFileSync(
      path.resolve(appTemplatesDirectoryPath, 'DokiDark.vstheme.template'),
      {encoding: 'utf-8'}
    )
    
    const themes = dokiThemes
      .filter(dokiTheme => dokiTheme.definition.dark);
    
    // write things for extension
    themes.forEach(dokiTheme => {
      fs.writeFileSync(
        path.resolve(generatedThemesDirectory, `${getName(dokiTheme.definition)}.vstheme`),
        darkTemplate
      )
    });
    
    const dokiThemeDefinitions = themes
      .map((dokiTheme) => {
        const dokiDefinition = dokiTheme.definition;
        return {
          information: omit(dokiDefinition, [
            "colors",
            "overrides",
            "ui",
            "icons",
          ]),
          colors: dokiTheme.appThemeDefinition.colors,
          stickers: dokiTheme.stickers,
        };
      })
      .reduce((accum: StringDictionary<any>, definition) => {
        accum[definition.information.id || ''] = definition;
        return accum;
      }, {});
    const finalDokiDefinitions = JSON.stringify(dokiThemeDefinitions);
    fs.writeFileSync(
      path.resolve(solutionsDirectory, "Resources", "DokiThemeDefinitions.json"),
      finalDokiDefinitions
    );

  })
  .then(() => {
    console.log("Theme Generation Complete!");
  });

function getName(dokiDefinition: MasterDokiThemeDefinition) {
  return dokiDefinition.name.replace(':', '');
}
