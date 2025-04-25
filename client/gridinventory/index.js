let browser = null;
let blockMap = false;

const openInventory = async () => {
    const inventoryData = await mp.events.callRemoteProc("Inventory:fetchData");
    if (inventoryData == null)
        return;
    browser = mp.browsers.new("package://browser/gridinventory/index.html");
    browser.execute(`loadInventoryData(${inventoryData});`);
    mp.gui.cursor.show(true, true);
    mp.gui.chat.activate(false);
    blockMap = true;
};

const closeInventory = () => {
    if (browser == null)
        return;
    browser.destroy();
    browser = null;
    mp.gui.cursor.show(false, false);
    setTimeout(() => blockMap = false, 250);
};

mp.keys.bind(0x49, true, async function() {
    if (browser != null || mp.game.ui.isPauseMenuActive() || blockMap)
        return;
    await openInventory();
});
mp.keys.bind(0x1B, true, closeInventory);

mp.events.add({
    'Inventory:onMoveItem': (itemId, sourceInvIdx, destInvIdx, newPos, rotated) => {
        mp.events.callRemote("Inventory:onMoveItem", itemId, sourceInvIdx, destInvIdx, newPos, rotated);
    },
    'render': () => {
        if (blockMap) {
            mp.game.controls.disableControlAction(2, 199, true);
            mp.game.controls.disableControlAction(2, 200, true);
        }
    }
})