import type { MaterialInfo } from '$lib/models/materialInfo';
import type { PageLoad } from './$types';

function generateMaterialTags(
    materialTable: Record<string, MaterialInfo[]>
  ): Record<number, string> {
    // Flatten: For each group and its array, create entries with group and item
    const entries = Object.entries(materialTable).flatMap(([group, items]) =>
      items.map((item) => ({
        group,
        item,
      }))
    );

    // Group by matId
    const grouped = entries.reduce(
      (acc, { group, item }) => {
        if (!acc[item.matId]) {
          acc[item.matId] = [];
        }
        acc[item.matId].push(`${group}&&&${item.qty}`);
        return acc;
      },
      {} as Record<number, string[]>
    );

    // Convert array to joined string with '||'
    const materialTags: Record<number, string> = {};
    for (const matIdStr in grouped) {
      materialTags[Number(matIdStr)] = grouped[matIdStr].join("||");
    }

    return materialTags;
  }

export const load: PageLoad = async ({ fetch }) => {
  const resourceurl = 'https://neeye0n.github.io/flux/ItemDescTableModder';
  const urls = [
    `${resourceurl}/BrewingMatsTable.json`,
    `${resourceurl}/CookingMatsTable.json`,
    `${resourceurl}/InstanceMatsTable.json`,
    `${resourceurl}/QuestMatsTable.json`
  ];

  const [brewRes, cookRes, instRes, questRes] = await Promise.all(urls.map((url) => fetch(url)));

  if (!brewRes.ok || !cookRes.ok || !instRes.ok || !questRes.ok) {
    throw new Error('One or more requests failed');
  }

  const [dataA, dataB, dataC, dataD] = await Promise.all([
    brewRes.json() as Promise<Record<string, MaterialInfo[]>>,
    cookRes.json() as Promise<Record<string, MaterialInfo[]>>,
    instRes.json() as Promise<Record<string, MaterialInfo[]>>,
    questRes.json() as Promise<Record<string, MaterialInfo[]>>
  ]);

  return {
    brewingTags: generateMaterialTags(dataA),
    cookingTags: generateMaterialTags(dataB),
    instanceTags: generateMaterialTags(dataC),
    questTags: generateMaterialTags(dataD)
  };
};