   M S B A M L   `   `   `  ����R  NMinecraftEarthTiles_GUI, Version=1.4.6.0, Culture=neutral, PublicKeyToken=nullA%clr-namespace:MinecraftEarthTiles_GUIMinecraftEarthTiles_GUI  5   	   -    'MinecraftEarthTiles_GUI.SelectionWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   _ [MaterialDesignThemes.Wpf, Version=4.6.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5   	   1local%clr-namespace:MinecraftEarthTiles_GUI  5   	   -   5          � Title$%  Minecraft Earth Tiles Selection��5	   	   $	��516��61   $	��953��6>   $	��516��6J   $	��953��6Z      MaterialDesignWindow#��[  5
   	    � WindowStartupLocation$ CenterScreen=�5   	   .��5      ]� .+�5   
   o� $	��Top=�6    # MaterialDesignToolBarBackground#���  6$   .@�5      m�  m� Icon 5      '  !MaterialDesignThemes.Wpf.PackIcon  5         Kind$
 Menu=�6.   .@�5      m� -   5        $��btnSave��5      $
��Save��6*    5        5      $ ContentSave=�62   5      m� 5      -   5        $��btnSaveClose��5      $��Save and close��6/    5        5      $ ContentSaveMove=�62   5      m� 5      -   5        $��Clear Selection��5       5        5      $ Delete=�62   5       m� 5!      -   5        $��Load Selection from file��5!       5"        5#      $ 
FileUpload=�62   5%      m� 5&      -   5        $��Save Selection to file��5&       5'        5(      $ FileDownload=�62   5*      �� 5+      m� 5,      -   5        $��Close��5,       5-        5.      $ Close=�62   50      51      m� 52      -   5        $
��Help��52       53        54      $
 Help=�6.   $��Center=�6:   56      57      ]� 58   
   $��Bottom=�6   #���  6,   .+�59      �� $��Right=�6    �� Orientation$ 
Horizontal=�60   $��10q�6I   .+�5:      �� -	   5         ��Name$ txb_toManyTiles��5:   -   
���  ��$�� ��6F   $��Center=�6N   � 5;      $��10��6   �� 5<      -
   5         ��Name$ btn_Save_Selection��5<      .��5=      �� $ 
Horizontal=�6!   .+�5>        $ ContentSave=�62   $��Center=�6E   �� 5?      Save6*   5@      5A      � 5B      $��10��6   �� 5C      -   5        $ btn_Cancel_Selection��5C      .��5D      �� $ 
Horizontal=�6!   .+�5E        $ Close=�62   $��Center=�6?   �� 5F      Close6+   5G      5H      5I      �� 5K      $
��Left=�6   $��Center=�64   $ Vertical=�6O   .+�5L      �� $ 
Horizontal=�6   .+�5M      �� -   5         ��Name$ lbl_Corners��5M      $2��,Corner Coordinates for worldborder / chunky:��61   �� 5N      -   5         ��Name$ txb_Corners��5N      $	��180��63   5O      5P      5Q      � 5R   
   	 � ColumnDefinitions	 5S      �� 5T      $	��200 �6#   �� 5U      $��* �6#   .+�5X      �� $��0��6   $��10q�6)   .+�5Y      �� -   5        $ lbl_ZoomSlider��5Y      $
��Zoom��60   �� 5Z      -   5        
 ��Name$
 zsl_ZoomSlider��5Z       �� Orientation$ 
Horizontal=�61    � Minimum$	 0.5Q�6J    � Maximum$
 10.0Q�6X    � LargeChange$	 0.5Q�6g    � SmallChange$
 0.25Q�6y    � Value$	 1.0Q�6�   �� 5[      -   5        $ lbl_background��5[      $��
Background��60   �� 5\      -   5         ��Name$ cbb_Background_Image��5\      .@�5]      ��  �� 
IsSelected . .��65   	Terrain6>   �� 5^      .��6#   	Borders6,   5_      �� 5`      -   5        $ lbl_Spawn_Tile��5`      $��
Spawn Tile��60   �� 5a      -   5        $ cbb_Spawn_Tile��5a      �� 5b      -   5        $ lbl_Void_Barrier��5b      $��Void Barrier (in Blocks)��62   �� 5c      -   5        $ cbb_Void_Barrier��5c      .@�5d      ��  . .��65   068   �� 5e      .��6#   5126(   �� 5f      .��6#   10246)   �� 5g      .��6#   15366)   �� 5h      .��6#   20486)   5i      �� 5j      -   5        $ btn_Clear_Selection��5j   +   $��0 10 0 0q�6   .��5k      �� $ 
Horizontal=�6!   .+�5l        $ Delete=�62   $��Center=�6@   $��5 0 0 0q�6[   �� 5m      Clear Selection65   5n      5o      5p      �� 5q      -   5         ��Name$ ScrollViewer��5q   +   $��1��6    �� PanningMode$
 Both=�6A   $��Visible=�6T   $��Visible=�6|     MaterialDesignScrollViewer#���  6�   .��5r      �� $
��Left=�6   $	��Top=�68   .+�5s      � -   5        $��Tiles��5s      $
��1440��6(   $	��720��65   $
��1440��6B   $	��720��6R   $��0,0,0,0q�6b   $	��Top=�6s   $
��Left=�6�    � LayoutTransform 5t      V� 5u      .~�5v   "   �� -   5        scaleTransform��5v   1   5w      .+�5y      �� -   5         ��Name$ img_Background��5y       $ ��../MyResources/terrain.jpg��68   $
��1440��6\   $	��720��6i   $
��1440��6v   $	��720��6�   $��0,0,0,0q�6�   $	��Top=�6�   $
��Left=�6�   � 5z      -   5        $��
CheckBoxes��5z      $
��1440��61   $	��720��6>   $
��1440��6K   $	��720��6[   $��0,0,0,0q�6k   $	��Top=�6|   $
��Left=�6�   5{       5|      �� 5}       �� ScaleX 6)   ��  �� ElementName$ 
ZoomSlider�� �� Path$ Value� �� ScaleY 6_   �� $ 
ZoomSlider��$ Value�5      5�      5�      5�      5�      