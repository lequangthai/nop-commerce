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
                $(".wrapper").removeClass("cbp-spmenu-push-toright");
                $(".navigation-mobile").removeClass("cbp-spmenu-open");
            }
            else{
                $(this).addClass("open");
                $(".wrapper").addClass("cbp-spmenu-push-toright");
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

        //=====show left menu mobile==========
 /*       var  showLeftPush = document.getElementById( 'btn-mobile-menu' );
        showLeftPush.onclick = function() {
            classie.toggle( this, 'active' );
            classie.toggle( body, 'cbp-spmenu-push-toright' );
            classie.toggle( menuLeft, 'cbp-spmenu-open' );
            disableOther( 'showLeftPush' );
        };
*/

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

