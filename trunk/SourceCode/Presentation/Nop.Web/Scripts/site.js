
function tab_click(index) {
    var total = 3;
    for (var i = 1; i <= total; i++) {
        $("#tab-header .tab" + i).removeClass("tab-active");
        $("#tab-content .tab" + i).removeClass("tab-active");
    }
    $("#tab-header .tab" + index).addClass("tab-active");
    $("#tab-content .tab" + index).addClass("tab-active");
}
