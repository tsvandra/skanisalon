export function useDragAndDrop() {

  /**
   * Kétszintű (kategóriák és rajtuk belüli elemek) lista újraindexelése.
   * Szolgáltatásokhoz és Képekhez is használható.
   */
  const reorderNestedItems = async (groups, itemsKey, updateCallback, updateCategoryLogic = null) => {
    let counter = 10;
    const promises = [];

    groups.forEach(group => {
      group[itemsKey].forEach(item => {
        let changed = false;

        // Sorrend frissítése
        if (item.orderIndex !== counter) {
          item.orderIndex = counter;
          changed = true;
        }

        // Kategória váltás ellenőrzése
        if (updateCategoryLogic) {
          const catChanged = updateCategoryLogic(item, group);
          if (catChanged) changed = true;
        }

        // Ha bármi változott, meghívjuk a mentést
        if (changed) {
          promises.push(updateCallback(item, false));
        }

        counter += 10;
      });
    });

    if (promises.length > 0) {
      await Promise.all(promises);
    }
  };

  /**
   * Egyszintű lista újraindexelése (pl. csak a kategóriák mappái).
   */
  const reorderFlatItems = async (items, updateCallback) => {
    let counter = 10;
    const promises = [];

    items.forEach(item => {
      if (item.orderIndex !== counter) {
        item.orderIndex = counter;
        promises.push(updateCallback(item, false));
      }
      counter += 10;
    });

    if (promises.length > 0) {
      await Promise.all(promises);
    }
  };

  return {
    reorderNestedItems,
    reorderFlatItems
  };
}
