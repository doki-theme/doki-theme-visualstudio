import {
  BaseAppDokiThemeDefinition,
  constructNamedColorTemplate,
  DokiThemeDefinitions,
  evaluateTemplates,
  fillInTemplateScript,
  MasterDokiThemeDefinition,
  resolvePaths,
  resolveTemplate,
  StringDictionary,
} from "doki-build-source";
import omit from 'lodash/omit';
import fs from "fs";
import path from "path";
import xmlParser from "xml2js";


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

const xmlBuilder = new xmlParser.Builder();

const toXml = (xml1: string) =>
  xmlParser.parseStringPromise(xml1)

console.log("Preparing to generate themes.");
const solutionsDirectory = path.resolve(repoDirectory, "doki-theme-visualstudio");

const generatedThemesDirectory = path.resolve(solutionsDirectory, 'Themes', 'generated')

if (!fs.existsSync(generatedThemesDirectory)) {
  fs.mkdirSync(generatedThemesDirectory, {recursive: true})
}

function getVSThemeName(dokiTheme: { path: string; appThemeDefinition: BaseAppDokiThemeDefinition; definition: MasterDokiThemeDefinition; stickers: { defaultSticker: { path: string; name: string } }; theme: {}; templateVariables: DokiThemeVisualStudio }) {
  return `${getName(dokiTheme.definition)}.vstheme`;
}

evaluateTemplates(
  {
    appName: 'visualstudio',
    currentWorkingDirectory: __dirname,
  },
  createDokiTheme
)
  .then(async (dokiThemes) => {

    const darkTemplate = fs.readFileSync(
      path.resolve(appTemplatesDirectoryPath, 'DokiDark.vstheme.template'),
      {encoding: 'utf-8'}
    )

    const csProjFilePath = path.resolve(solutionsDirectory, 'doki-theme-visualstudio.csproj');
    const csProjFile = await toXml(
      fs.readFileSync(
        csProjFilePath,
        {encoding: 'utf-8'}
      )
    )

    csProjFile.Project.ItemGroup = csProjFile.Project.ItemGroup.map(
      (itemGroup: any) => {
        if (!!itemGroup.None) {
          return {
            None: [
              ...itemGroup.None.filter(
                (none: any) => !none.$.Include.startsWith('Themes\generated'),
              ),
              ...dokiThemes.map(dokiTheme => ({
                '$': {Include: `Themes\generated\\${getVSThemeName(dokiTheme)}`},
                SubType: ['Designer']
              }))
            ]
          }
        }

        return itemGroup
      }
    )


    const xml = xmlBuilder.buildObject(csProjFile);
    fs.writeFileSync(csProjFilePath, xml, 'utf8');

    const themes = dokiThemes
      .filter(dokiTheme => dokiTheme.definition.dark);

    // write things for extension
    await themes.reduce((accum, dokiTheme) => {
      return accum.then(async () => {
        const template = await resolveVisualStudioThemeTemplate(darkTemplate, dokiTheme);
        fs.writeFileSync(
          path.resolve(generatedThemesDirectory, getVSThemeName(dokiTheme)),
          template
        )
      })
    }, Promise.resolve());

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

async function resolveVisualStudioThemeTemplate(darkTemplate: string, dokiTheme: { path: string; definition: MasterDokiThemeDefinition; stickers: { secondary?: { path: string; name: string; } | undefined; defaultSticker: { path: string; name: string; }; }; templateVariables: DokiThemeVisualStudio; theme: {}; appThemeDefinition: BaseAppDokiThemeDefinition; }): Promise<string> {
  const filledInTemplate = fillInTemplateScript(darkTemplate, dokiTheme.templateVariables);
  const templateAsXml = await toXml(filledInTemplate)
  const themeElement = templateAsXml.Themes.Theme[0];
  themeElement.$.Name = getName(dokiTheme.definition);
  themeElement.$.GUID = `{${dokiTheme.definition.id}}`
  return xmlBuilder.buildObject(templateAsXml)
}

