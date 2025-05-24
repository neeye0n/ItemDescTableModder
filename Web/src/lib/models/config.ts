export interface ConfigFile {
  itemIdDescTextColor: string;
  itemIdDescValueColor: string;
  brewingConfig: TaggingConfig;
  cookingConfig: TaggingConfig;
  questConfig: TaggingConfig;
  instanceConfig: TaggingConfig;
}

export interface TaggingConfig {
  enableTags: number;
  enableDescriptions: number;
  tagText: string;
  descriptionHeaderColor: string;
  descriptionRowsColor: string;
}
