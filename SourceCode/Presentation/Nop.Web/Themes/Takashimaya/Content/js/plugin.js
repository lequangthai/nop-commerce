/**
 * Created by ntatuan on 30/03/2015.
 */








(function ($) {

    // center fixed
    jQuery.fn.centerFixed = function () {
        this.css("position","fixed");
         this.css("top", Math.max(0, (($(window).height() - $(this).outerHeight()) / 2) +
                $(window).scrollTop()) + "px");
     //   this.css("left", Math.max(0, (($(window).width() - $(this).outerWidth()) / 2) +
    //    $(window).scrollLeft()) + "px");
        return this;
    }

    // center
    jQuery.fn.center = function(parent) {
        if (parent) {
            parent = this.parent();
        } else {
            parent = window;
        }
        this.css({
            "position": "absolute",
            "top": ((($(parent).height() - this.outerHeight()) / 2) + $(parent).scrollTop() + "px"),
            "left": ((($(parent).width() - this.outerWidth()) / 2) + $(parent).scrollLeft() + "px")
        });
        return this;
    }




    $(document).ready(function() {


        var heightBody=$(window).height();



       $(".navigation-top > #nav-top > ul > li").hoverIntent({

            sensitivity: 1, // number = sensitivity threshold (must be 1 or higher)
            interval: 30,  // number = milliseconds for onMouseOver polling interval
            timeout: 100,   // number = milliseconds delay before onMouseOut
            over:function(){

                if(!$(this).parents(".navigation-top").hasClass("active-mobile"))
                    $(this).addClass("hover");
            },
            out: function(){
                if(!$(this).parents(".navigation-top").hasClass("active-mobile"))
                    $(this).removeClass("hover");
            }
        });


        // menu button click
        $(".toggleBox .navToggle").on("click", function () {
            if($(this).hasClass("open"))
            {
                $(this).removeClass("open");
                $(".wrapper-content").removeClass("cbp-spmenu-push-toright");
                $(".navigation-mobile").removeClass("cbp-spmenu-open");
            }
            else{
                $(this).addClass("open");
                $(".wrapper-content").addClass("cbp-spmenu-push-toright");
                $(".navigation-mobile").addClass("cbp-spmenu-open");

            }
        });



        // sub menu click
       $("#nav-top li.has-sub > a").click(function(){


            var t=$(this).parent("li");


            if(t.hasClass("active"))
            {
                $(this).next(".nav-sub:first").slideUp(function(){
                    t.removeClass("active");
                    $(this).removeAttr("style");
                });

            }
            else
            {


                $(".nav-sub").not(t).slideUp(function(){
                    $(this).removeAttr("style");
                });

                $(this).next(".nav-sub:first").slideDown(function(){
                    $(".navigation-top.active-mobile #nav-top li").removeClass("active");
                    t.addClass("active");
                    $(this).removeAttr("style");
                });

            }

        });



        $(".menu-category > li > a").click(function(){
            var t = $(this).parent("li");
            var child =$(this);


            if (t.hasClass("active")) {
                $(this).next("ul:first").slideUp(function () {
                    t.removeClass("active");
                    $(this).removeAttr("style");
                });
            }
            else {
                $(".menu-category > li > ul").not(t).slideUp(function () {
                    $(this).removeAttr("style");
                    $(".menu-category > li > a").not(child).removeClass("active");
                });


                $(this).next("ul:first").slideDown(function () {
                    $(".menu-category > li").removeClass("active");
                    t.addClass("active");
                    $(this).removeAttr("style");
                });
            }

        });


        //======mobile menu click=========
        $(".mobile-menu > li > a").click(function(){
            var t = $(this).parent("li");
            var child =$(this);


            if (t.hasClass("active")) {
                $(this).next("ul:first").slideUp(function () {
                    t.removeClass("active");
                    $(this).removeAttr("style");
                });
            }
            else {
                $(".mobile-menu > li > ul").not(t).slideUp(function () {
                    $(this).removeAttr("style");
                    $(".mobile-menu > li").not(child).removeClass("active");
                });


                $(this).next("ul:first").slideDown(function () {
                    $(".mobile-menu > li").removeClass("active");
                    t.addClass("active");
                    $(this).removeAttr("style");
                });
            }

        });


        //=====brand scroll====

        //$(".brand-category .content-brand").tinyscrollbar();

        $(".title-brand").click(function(){
            var parent=$(this).parent(".brand-category");
            if(parent.hasClass("active"))
            {
                $(this).next(".content-brand").slideUp(function(){
                    parent.removeClass("active");
                });
            }
            else{
                $(this).next(".content-brand").slideDown(function(){
                    parent.addClass("active");
                });
            }

        });


        //========banner================
        $('.carousel-style-1').owlCarousel({
            singleItem : true,
            items:1,
            navigation: true,
            pagination:true,
            slideSpeed : 500,
            margin: 10,
            nav:true,
            paginationSpeed : 500,
            loop: true,
            dots: true,
            responsiveRefreshRate : 200,
            navText: ['<i class="glyphicon glyphicon-menu-left"></i>','<i class="glyphicon glyphicon-menu-right"></i>']

        });


        //======rate=======
        $('.rate').rating();


        //====function====add rate===========
        $(".stars a").click(function(){

            var star=$(this).attr("title");
            var id=$(this).parents("li").attr("id");

            alert(id);
            alert(star);

            // process ajax vote here

        });


        //=====show left menu mobile==========
 /*       var  showLeftPush = document.getElementById( 'btn-mobile-menu' );
        showLeftPush.onclick = function() {
            classie.toggle( this, 'active' );
            classie.toggle( body, 'cbp-spmenu-push-toright' );
            classie.toggle( menuLeft, 'cbp-spmenu-open' );
            disableOther( 'showLeftPush' );
        };
*/


        //=======slider product===================
        $('.carousel-style-2').owlCarousel({
            singleItem : true,
            items:4,

            responsive: {
                0:{
                    items:3, // In this configuration 1 is enabled from 0px up to 479px screen size
                    nav:true
                },

                480:{
                    items:4, // from 480 to 677
                    nav:true // from 480 to max
                }


            },



            navigation: true,
            pagination:true,
            slideSpeed : 500,
            margin: 10,
            nav:true,
            paginationSpeed : 500,
            loop: true,
            dots: true,
            responsiveRefreshRate : 200,
            navText: ['<i class="glyphicon glyphicon-menu-left"></i>','<i class="glyphicon glyphicon-menu-right"></i>']

        });

        $(".carousel-style-2 .item a").click(function(){
            var parent=$(this).parent(".item");
            var src=$(this).attr("data-img");
            $(".carousel-style-2 .item").removeClass("active");
            parent.addClass("active");

            $(".product-images .image img").attr("src",src);

        });


        // ============click tab==============

        //$(".tab-pane .content:eq(0)").tinyscrollbar();

        //$(".tab ul li").click(function(event){
        //    event.preventDefault();
        //    var index=$(this).index();
        //    var parent=$(this).parents(".tab");

        //    parent.find(".nav-tabs li").not($(this)).removeClass("active");
        //    $(this).addClass("active");

        //    parent.find(".tab-content .tab-pane").not(":eq("+ index +")").removeClass("active");
        //    parent.find(".tab-content .tab-pane:eq("+ index +")").addClass("active");

        //   $(".tab-pane .content:eq("+ index +")").tinyscrollbar();


        //});






        //========send mail click==========
        $(".btn-sendmail").click(function(){
            var content = $('.alert-popup .content');

            $(".alert-popup").bPopup({
                onOpen: function() {
                    content.html("test");
                },
                onClose: function() {
                    content.empty();
                }
            });
        });




    });


    $( window ).resize(function() {


        var heightBody=$(window).height();


    });

})(jQuery);

