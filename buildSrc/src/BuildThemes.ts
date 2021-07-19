import {
  BaseAppDokiThemeDefinition,
  constructNamedColorTemplate,
  DokiThemeDefinitions,
  evaluateTemplates,
  fillInTemplateScript,
  MasterDokiThemeDefinition,
  resolvePaths,
  resolveTemplate,
  StringDictionary, walkDir,
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
      [colorName]: colorValue.startsWith('#') ?
        colorValue.substr(1) : colorValue,
    }), {});
  const finalColors: StringDictionary<string> = {
    ...cleanedColors,
    ...colorsOverride
  };
  return {
    ...finalColors,
    editorAccentColor:
      dokiThemeDefinition.overrides?.editorScheme?.colors?.accentColor?.substr(1) ||
      finalColors.accentColor
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
      stickers: getStickers(
        masterThemeDefinition,
        masterThemeDefinitionPath,
        appThemeDefinition
      ),
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
  themePath: string,
  dokiTheme: AppDokiThemeDefinition,
) => {
  const secondary =
    dokiDefinition.stickers.secondary || dokiDefinition.stickers.normal;
  const backgrounds = dokiTheme.backgrounds;
  return {
    defaultSticker: {
      path: resolveStickerPath(themePath, dokiDefinition.stickers.default),
      name: dokiDefinition.stickers.default,
      anchoring: backgrounds?.default?.anchor || "center",
      opacity: backgrounds?.default?.opacity || (dokiDefinition.dark ? 0.05 : 0.1)
    },
    ...(secondary
      ? {
        secondary: {
          path: resolveStickerPath(themePath, secondary),
          name: secondary,
          anchoring: backgrounds?.secondary?.anchor || "center",
          opacity: backgrounds?.secondary?.opacity || (dokiDefinition.dark ? 0.05 : 0.1)
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
} else {
  fs.readdirSync(generatedThemesDirectory).forEach(
    generatedThemePath => fs.rmSync(path.resolve(generatedThemesDirectory, generatedThemePath))
  )
}

function getVSThemeName(dokiTheme: { path: string; appThemeDefinition: BaseAppDokiThemeDefinition; definition: MasterDokiThemeDefinition; stickers: { defaultSticker: { path: string; name: string } }; theme: {}; templateVariables: DokiThemeVisualStudio }) {
  return `${getName(dokiTheme.definition)}.vstheme`;
}

function getXMLTemplates() {
  return walkDir(appTemplatesDirectoryPath)
    .then(paths => {
      return paths.filter(pathGuy => pathGuy.endsWith("vstheme.template"))
    }).then(templatePaths => {
      return templatePaths.reduce((accum, next) =>
        accum.then(async (templates) => {
          const themeXML = await toXml(
            fs.readFileSync(
              next,
              {encoding: 'utf-8'}
            )
          );
          const themeName = themeXML.Themes.Theme[0].$.Name;
          return {
            ...templates,
            [themeName]: themeXML,
          }
        }), Promise.resolve({} as StringDictionary<any>));
    });
}

evaluateTemplates(
  {
    appName: 'visualstudio',
    currentWorkingDirectory: __dirname,
  },
  createDokiTheme
)
  .then(async (dokiThemes) => {
    const specificTheme = process.argv[2];
    const specifiedThemes = process.argv.slice(2);
    const themes = dokiThemes
      .filter(dokiTheme => dokiTheme.definition.dark)
      .filter(
        dokiTheme => !specificTheme ||
          specifiedThemes.findIndex(
            themeId => themeId === dokiTheme.definition.id
          ) > -1
      );

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
              ...themes.map(dokiTheme => ({
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


    const darkTemplate = fs.readFileSync(
      path.resolve(appTemplatesDirectoryPath, 'DokiDark.vstheme.template'),
      {encoding: 'utf-8'}
    )

    const templates = await getXMLTemplates();
    
    console.log(templates);

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
          colors: {
            textEditorBackground: dokiTheme.templateVariables["textEditorBackground"],
            accentColor: dokiTheme.templateVariables["accentColor"],
          },
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

