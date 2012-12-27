

// menu item from 0->n

// verticalmenu("menuItem",8)
function verticalmenu(menuPrefix,menuItemCount) {
    for (var i = 0; i < menuItemCount; i++) {
        verticalmenuItem(menuPrefix + i.toString(), menuPrefix + i + "_sub");
        
    }
}
// verticalmenuItem("menuItem1","menuItem1_sub")
// verticalmenuItem("menuItem2","menuItem2_sub")
function verticalmenuItem(fathermenuId, childmenuId) {
    $("#"+childmenuId).hide();

    $("#" + fathermenuId).click(function () {
        if ($(this).hasClass("menu_on")) {
            $("#" + childmenuId).slideUp(500);
            $(this).removeClass("menu_on");
            $(this).addClass("menu_off");
        } else if ($(this).hasClass("menu_off")) {
            $("#" + childmenuId).slideDown(500);
            $(this).removeClass("menu_off");
            $(this).addClass("menu_on");
        }

    });
    
}

function GenerateVerticalMenuJS(menuName, count) {
    verticalmenu(menuName, count);
}