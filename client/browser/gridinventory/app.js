
let inventories = [];
let draggedItem = null;
let offsetX, offsetY;
let lastMouseX = 0;
let lastMouseY = 0;

function createInventory(rows, cols, size = 40, title = "Inventory", maxWeight = 40.00) {
    const $wrapper = $('#inventory-wrapper');
    const $header = $('<div>').addClass('inventory-header');
    const $container = $('<div>').addClass('inventory-container');
    const $grid = $('<div>').addClass('grid');
    const $title = $('<div>').addClass('inventory-title').text(title);
    const $weight = $('<div>').addClass('inventory-weight').text(`0.00kg / ${maxWeight.toFixed(2)}kg`);

    $container.append($grid);
    $header.append($container).append($title).append($weight);
    $wrapper.append($header);

    const inventory = {
        container: $container[0],
        grid: $grid[0],
        cellSize: size,
        gridRows: rows,
        gridCols: cols,
        items: [],
        currentWeight: 0.00,
        maxWeight: maxWeight
    };

    $container.css({
        width: `${cols * size}px`,
        height: `${rows * size}px`
    });
    $grid.css({
        width: `${cols * size}px`,
        height: `${rows * size}px`,
        gridTemplateColumns: `repeat(${cols}, ${size}px)`,
        gridTemplateRows: `repeat(${rows}, ${size}px)`
    });

    for (let i = 0; i < rows * cols; i++) {
        const $cell = $('<div>').addClass('cell').css({
            width: `${size}px`,
            height: `${size}px`
        });
        $grid.append($cell);
    }

    inventories.push(inventory);
    return inventories.length - 1;
}

function isSpaceFree(inventoryIdx, x, y, width, height, excludeItem = null) {
    const inv = inventories[inventoryIdx];
    if (x < 0 || y < 0 || x + width > inv.gridCols || y + height > inv.gridRows) {
        return { free: false, collidedItem: null };
    }
    for (let item of inv.items) {
        if (item === excludeItem) continue;
        if (!(x + width <= item.x || x >= item.x + item.width ||
            y + height <= item.y || y >= item.y + item.height)) {
            return { free: false, collidedItem: item };
        }
    }
    return { free: true, collidedItem: null };
}

function updateWeight(inventoryIdx) {
    const inv = inventories[inventoryIdx];
    $(inv.container).parent().find('.inventory-weight').text(`${inv.currentWeight.toFixed(2)}kg / ${inv.maxWeight.toFixed(2)}kg`);
}

function createItem(inventoryIdx, item) {
    const inv = inventories[inventoryIdx];
    const $item = $('<div>').addClass('item');

    const $name = $('<div>').css({
        position: 'absolute',
        top: '2px',
        right: '4px',
        color: '#fff',
        fontSize: '11px',
        pointerEvents: 'none'
    });

    const $amount = $('<div>').css({
        position: 'absolute',
        bottom: '2px',
        right: '4px',
        color: '#fff',
        fontSize: '11px',
        pointerEvents: 'none'
    });

    $name.text(item.ShortName || "Item");
    $amount.text(item.Amount);
    $item.append($name).append($amount);

    let width = item.RowsColumns[1];
    let height = item.RowsColumns[0];

    if (item.IsRotated) {
        const tmp = width;
        width = height;
        height = tmp;
        $item[0].iconRotation = 90;
    } else {
        $item[0].iconRotation = 0;
    }

    $item[0].width = width;
    $item[0].height = height;
    $item[0].x = item.Column;
    $item[0].y = item.Row;
    $item[0].lastValidWidth = width;
    $item[0].lastValidHeight = height;
    $item[0].inventoryIdx = inventoryIdx;
    $item[0].weight = item.Weight || 0.5;
    $item[0].isRotated = item.IsRotated;
    $item[0].instanceId = item.InstanceId;

    updateItemStyle($item, inv.cellSize);

    $item.on('mousedown', function(e) {
        if (e.button === 0) {
            draggedItem = this;
            draggedItem.originalX = this.x;
            draggedItem.originalY = this.y;
            draggedItem.originalRotation = this.isRotated;
            draggedItem.originalWidth = this.width;
            draggedItem.originalHeight = this.height;
            draggedItem.sourceInventoryIdx = this.inventoryIdx;
            draggedItem.instanceId = this.instanceId;
            const rect = this.getBoundingClientRect();
            offsetX = e.clientX - rect.left;
            offsetY = e.clientY - rect.top;
            lastMouseX = e.clientX;
            lastMouseY = e.clientY;
            $(this).css('z-index', 1000);
            $(this).css({ left: `${e.clientX - offsetX}px`, top: `${e.clientY - offsetY}px` });
            $('body').append(this);
        }
    });

    $item.on('contextmenu', function(e) {
        e.preventDefault();
    });

    $(inv.container).append($item);
    inv.items.push($item[0]);
    inv.currentWeight += $item[0].weight;
    updateWeight(inventoryIdx);
    return $item[0];
}

function updateItemStyle($item, cellSize) {
    $item.css({
        left: `${$item[0].x * cellSize}px`,
        top: `${$item[0].y * cellSize}px`,
        width: `${$item[0].width * cellSize - 4}px`,
        height: `${$item[0].height * cellSize - 4}px`
    });
    $item.find('img').css({
        transform: `rotate(${$item[0].iconRotation}deg)`
    });
}

$(document).on('mousemove', function(e) {
    if (!draggedItem) return;
    lastMouseX = e.clientX;
    lastMouseY = e.clientY;
    $(draggedItem).css({
        left: `${e.clientX - offsetX}px`,
        top: `${e.clientY - offsetY}px`
    });
    $(draggedItem).find('img').css({
        transform: `rotate(${draggedItem.iconRotation}deg)`
    });

    inventories.forEach((inv, idx) => {
        const rect = inv.container.getBoundingClientRect();
        const gridX = Math.round((e.clientX - rect.left - offsetX) / inv.cellSize);
        const gridY = Math.round((e.clientY - rect.top - offsetY) / inv.cellSize);
        highlightCells(idx, gridX, gridY, draggedItem.width, draggedItem.height);
    });
});

$(document).on('mouseup', function(e) {
    if (!draggedItem) return;
    let placed = false;
    const sourceInvIdx = draggedItem.sourceInventoryIdx;
    const origRotation = draggedItem.originalRotation;

    for (let i = 0; i < inventories.length; i++) {
        const inv = inventories[i];
        const rect = inv.container.getBoundingClientRect();
        const gridX = Math.round((e.clientX - rect.left - offsetX) / inv.cellSize);
        const gridY = Math.round((e.clientY - rect.top - offsetY) / inv.cellSize);
        const { free } = isSpaceFree(i, gridX, gridY, draggedItem.width, draggedItem.height, draggedItem);
        if (free &&
                e.clientX >= rect.left && e.clientX <= rect.right &&
                e.clientY >= rect.top && e.clientY <= rect.bottom &&
                (sourceInvIdx === i ? inv.currentWeight : inv.currentWeight + draggedItem.weight) 
            <= inv.maxWeight) {

            inventories[sourceInvIdx].items = inventories[sourceInvIdx].items.filter(item => item.instanceId !== draggedItem.instanceId);
            inventories[sourceInvIdx].currentWeight -= draggedItem.weight;
            updateWeight(sourceInvIdx);

            draggedItem.x = gridX;
            draggedItem.y = gridY;
            draggedItem.inventoryIdx = i;
            draggedItem.lastValidWidth = draggedItem.width;
            draggedItem.lastValidHeight = draggedItem.height;
            updateItemStyle($(draggedItem), inv.cellSize);

            inv.items.push(draggedItem);
            inv.currentWeight += draggedItem.weight;
            updateWeight(i);
            $(inv.container).append(draggedItem);

            if (sourceInvIdx !== i || draggedItem.originalX !== gridX || draggedItem.originalY !== gridY || draggedItem.isRotated !== origRotation) {
                mp.trigger("Inventory:onMoveItem", draggedItem.instanceId, sourceInvIdx, i, JSON.stringify([gridY, gridX]), draggedItem.isRotated);
            }
            placed = true;
            break;
        }
    }

    if (!placed) {
        const inv = inventories[draggedItem.inventoryIdx];
        draggedItem.x = draggedItem.originalX;
        draggedItem.y = draggedItem.originalY;
        if (draggedItem.width !== draggedItem.lastValidWidth || draggedItem.height !== draggedItem.lastValidHeight) {
            draggedItem.width = draggedItem.originalWidth;
            draggedItem.height = draggedItem.originalHeight;
            draggedItem.isRotated = origRotation;
            draggedItem.iconRotation = origRotation ? 90 : 0;
        }
        updateItemStyle($(draggedItem), inv.cellSize);
        $(inv.container).append(draggedItem);
        if (draggedItem.isRotated !== origRotation) {
            draggedItem.lastValidWidth = draggedItem.width;
            draggedItem.lastValidHeight = draggedItem.height;
            mp.trigger("Inventory:onMoveItem", draggedItem.instanceId, sourceInvIdx, draggedItem.inventoryIdx, JSON.stringify([draggedItem.y, draggedItem.x]), draggedItem.isRotated);
        }
    }

    inventories.forEach((inv, idx) => clearHighlights(idx));
    draggedItem = null;
});

$(document).on('mouseleave', function(e) {
    if (draggedItem) {
        const inv = inventories[draggedItem.sourceInventoryIdx];
        draggedItem.x = draggedItem.originalX;
        draggedItem.y = draggedItem.originalY;
        updateItemStyle($(draggedItem), inv.cellSize);
        $(inv.container).append(draggedItem);
        draggedItem = null;
    }
});

$(document).on('keydown', function(e) {
    if (e.key.toLowerCase() === 'r' && draggedItem) {
        e.preventDefault();
        const inv = inventories[draggedItem.inventoryIdx];
        const oldWidth = draggedItem.width;
        const oldHeight = draggedItem.height;
        draggedItem.width = draggedItem.height;
        draggedItem.height = oldWidth;
        draggedItem.iconRotation = draggedItem.iconRotation === 0 ? 90 : 0;
        draggedItem.isRotated = draggedItem.iconRotation === 90;

        const mouseX = lastMouseX - document.body.getBoundingClientRect().left;
        const mouseY = lastMouseY - document.body.getBoundingClientRect().top;
        const relativeX = offsetX / (oldWidth * inv.cellSize - 4);
        const relativeY = offsetY / (oldHeight * inv.cellSize - 4);

        $(draggedItem).css({
            width: `${draggedItem.width * inv.cellSize - 4}px`,
            height: `${draggedItem.height * inv.cellSize - 4}px`,
            left: `${mouseX - (relativeX * (draggedItem.width * inv.cellSize - 4))}px`,
            top: `${mouseY - (relativeY * (draggedItem.height * inv.cellSize - 4))}px`
        });

        $(draggedItem).find('img').css({
            transform: `rotate(${draggedItem.iconRotation}deg)`
        });

        offsetX = relativeX * (draggedItem.width * inv.cellSize - 4);
        offsetY = relativeY * (draggedItem.height * inv.cellSize - 4);

        inventories.forEach((inv, idx) => {
            const rect = inv.container.getBoundingClientRect();
            const gridX = Math.round((lastMouseX - rect.left - offsetX) / inv.cellSize);
            const gridY = Math.round((lastMouseY - rect.top - offsetY) / inv.cellSize);
            highlightCells(idx, gridX, gridY, draggedItem.width, draggedItem.height);
        });
    }
});

function highlightCells(inventoryIdx, x, y, width, height) {
    const inv = inventories[inventoryIdx];
    clearHighlights(inventoryIdx);
    const isValid = isSpaceFree(inventoryIdx, x, y, width, height, draggedItem).free;
    for (let i = x; i < x + width && i < inv.gridCols && i >= 0; i++) {
        for (let j = y; j < y + height && j < inv.gridRows && j >= 0; j++) {
            const index = j * inv.gridCols + i;
            if (index >= 0 && index < inv.gridRows * inv.gridCols) {
                $(inv.grid).children().eq(index).addClass(isValid ? 'highlight-valid' : 'highlight-invalid');
            }
        }
    }
}

function clearHighlights(inventoryIdx) {
    const inv = inventories[inventoryIdx];
    $(inv.grid).children().removeClass('highlight-valid highlight-invalid');
}

function loadInventoryData(inventoryDataArray) {
    $('#inventory-wrapper').empty();
    inventories = [];

    inventoryDataArray.forEach(inventoryData => {
        inventoryData = JSON.parse(inventoryData);

        let invIdx = createInventory(
            inventoryData.Rows,
            inventoryData.Columns,
            40,
            inventoryData.Title,
            inventoryData.MaxWeight
        );

        if (inventoryData.Items) {
            inventoryData.Items.forEach(item => {
                createItem(invIdx, item);
            });
        }
    });
}
