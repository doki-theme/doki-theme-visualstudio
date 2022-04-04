import {
  BaseAppDokiThemeDefinition,
  composeTemplateWithCombini,
  constructNamedColorTemplate,
  DokiThemeDefinitions,
  evaluateTemplates,
  fillInTemplateScript,
  MasterDokiThemeDefinition,
  resolvePaths,
  Sticker,
  StringDictionary, walkDir,
} from "doki-build-source";
import omit from 'lodash/omit';
import deepClone from 'lodash/cloneDeep';
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
    ...dokiThemeAppDefinition.colors,
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
) => {
  const secondary =
    dokiDefinition.stickers.secondary || dokiDefinition.stickers.normal;
  return {
    defaultSticker: {
      path: resolveStickerPath(themePath, dokiDefinition.stickers.default.name),
      name: dokiDefinition.stickers.default.name,
      anchoring: dokiDefinition.stickers.default.anchor || "center",
      opacity: (dokiDefinition.stickers.default.opacity / 100) || (dokiDefinition.dark ? 0.05 : 0.1)
    },
    ...(secondary
      ? {
        secondary: {
          path: resolveStickerPath(themePath, dokiDefinition.stickers.secondary!!.name),
          name: dokiDefinition.stickers?.secondary?.name || 'ayyLmao',
          anchoring: dokiDefinition.stickers?.secondary?.anchor || "center",
          opacity: (dokiDefinition.stickers.secondary?.opacity || (dokiDefinition.dark ? 5 : 10)) / 100
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
  fs.mkdirSync(generatedThemesDirectory, { recursive: true })
} else {
  fs.readdirSync(generatedThemesDirectory).forEach(
    generatedThemePath => fs.rmSync(path.resolve(generatedThemesDirectory, generatedThemePath))
  )
}

function getVSThemeName(dokiTheme: MasterDokiThemeDefinition) {
  return `${getName(dokiTheme)}.vstheme`;
}

function getThemeName(themeXML: any) {
  return themeXML.Themes.Theme[0].$.Name;
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
              { encoding: 'utf-8' }
            )
          );
          const themeName = getThemeName(themeXML);
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
        { encoding: 'utf-8' }
      )
    )

    csProjFile.Project.ItemGroup = csProjFile.Project.ItemGroup.map(
      (itemGroup: any) => {
        if (!!itemGroup.None) {
          return {
            None: [
              ...itemGroup.None.filter(
                (none: any) => !none.$.Include.startsWith('Themes\\generated'),
              ),
              ...themes.map(dokiTheme => ({
                '$': { Include: `Themes\\generated\\${getVSThemeName(dokiTheme.definition)}` },
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


    const templates = await getXMLTemplates();
    await themes.reduce((accum, dokiTheme) => {
      return accum.then(async () => {
        const template = await resolveVisualStudioThemeTemplate(templates, dokiTheme);
        fs.writeFileSync(
          path.resolve(generatedThemesDirectory, getVSThemeName(dokiTheme.definition)),
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

async function resolveVisualStudioThemeTemplate(xmlTemplates: StringDictionary<any>,
  dokiTheme: { path: string; definition: MasterDokiThemeDefinition; stickers: { secondary?: { path: string; name: string; opacity: number } | undefined; defaultSticker: { path: string; name: string; opacity: number}; }; templateVariables: DokiThemeVisualStudio; theme: {}; appThemeDefinition: BaseAppDokiThemeDefinition; }): Promise<string> {
  const evalutedTemplate = evaluateXmlTemplates(xmlTemplates, dokiTheme)
  const filledInTemplate = fillInTemplateScript(evalutedTemplate, dokiTheme.templateVariables);
  const templateAsXml = await toXml(filledInTemplate)
  const themeElement = templateAsXml.Themes.Theme[0];
  themeElement.$.Name = getName(dokiTheme.definition);
  themeElement.$.GUID = `{${dokiTheme.definition.id}}`
  return xmlBuilder.buildObject(templateAsXml)
}

// now kith
function smashXmlTemplatesTogether(parentXml: any, childXml: any): any {
   if (childXml.composeHax) {
    return parentXml;
  }
  
  const newParentXml = deepClone(parentXml);
  const newChildXml = deepClone(childXml);

  const parentStuff = newParentXml.Themes.Theme[0]
  const childStuff = newChildXml.Themes.Theme[0]

  const childCategories = childStuff.Category.reduce((accum: StringDictionary<any>, category: any) => ({
    ...accum,
    [category.$.GUID]: category,
  }), {} as StringDictionary<any>);
  parentStuff.Category = parentStuff.Category.map((parentCategory: any) => {
    const childCategory = childCategories[parentCategory.$.GUID];
    if (childCategory) {
      const childColors = childCategory.Color.reduce((accum: StringDictionary<any>, color: any) => ({
        ...accum,
        [color.$.Name]: color
      }), {});

      // exclude matching child template colors & add child colors in preference
      parentCategory.Color = parentCategory.Color.filter((parentColor: any) => {
        const childColor = childColors[parentColor.$.Name];
        return !childColor;
      }).concat(childCategory.Color);
    }
    return parentCategory;
  });

  return newParentXml;
}

function evaluateXmlTemplates(xmlTemplates: StringDictionary<any>, dokiTheme: { path: string; definition: MasterDokiThemeDefinition; stickers: { secondary?: { path: string; name: string; } | undefined; defaultSticker: { path: string; name: string; }; }; templateVariables: DokiThemeVisualStudio; theme: {}; appThemeDefinition: BaseAppDokiThemeDefinition; }): string {
  const childTemplateName = dokiTheme.appThemeDefinition.laf?.extends ||
    (dokiTheme.definition.dark ? 'dark' : 'light');

  // hax to get around the initial child composing
  // many parent themes.
  const childTemplate = {
    composeHax: true,
    Themes: {
      Theme: [
        {
          $: {
            BaseGUID: childTemplateName
          }
        }
      ]
    }
  };
  const resolvedXmlObject = composeTemplateWithCombini(
    childTemplate,
    xmlTemplates,
    template => template,
    templateXml => {
      const baseNodeAttributes = templateXml.Themes.Theme[0].$;
      const parentName: string = baseNodeAttributes.BaseGUID ||
        baseNodeAttributes.FallbackId;
      return parentName.startsWith('{') ? undefined :
        parentName.split(',')
          .map(parent => parent.trim() as string)
          .filter(Boolean);
    },
    smashXmlTemplatesTogether
  )
  return xmlBuilder.buildObject(resolvedXmlObject);
}
