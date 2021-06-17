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
} = resolvePaths(__dirname);

// todo: dis
type DokiThemeJupyter = {
  [k: string]: any;
};


function buildTemplateVariables(
  dokiThemeDefinition: MasterDokiThemeDefinition,
  masterTemplateDefinitions: DokiThemeDefinitions,
  dokiThemeAppDefinition: AppDokiThemeDefinition,
): DokiThemeJupyter {
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
  );
}

const getStickers = (
  dokiDefinition: MasterDokiThemeDefinition,
  themePath: string
) => {
  const secondary =
    dokiDefinition.stickers.secondary || dokiDefinition.stickers.normal;
  return {
    default: {
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
const themesDirectory = path.resolve(repoDirectory, "src", "dokithemejupyter");

evaluateTemplates(
  {
    appName: 'jupyter',
    currentWorkingDirectory: __dirname,
  },
  createDokiTheme
)
  .then((dokiThemes) => {

    // write things for extension
    const dokiThemeDefinitions = dokiThemes
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
        accum[definition.information.id] = definition;
        return accum;
      }, {});
    const finalDokiDefinitions = JSON.stringify(dokiThemeDefinitions);
    fs.writeFileSync(
      path.resolve(repoDirectory, "src", "DokiThemeDefinitions.ts"),
      `export default ${finalDokiDefinitions};`
    );

  })
  .then(() => {
    console.log("Theme Generation Complete!");
  });
