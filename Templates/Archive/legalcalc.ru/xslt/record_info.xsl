<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   

	<xsl:template match="common_data">				
			<tr><td>Кадастровый номер: </td> <td><xsl:value-of select = "cad_number"/></td></tr>			
			<tr><td>Номер кадастрового квартала: </td> <td><xsl:value-of select = "quarter_cad_number"/></td></tr>									
    </xsl:template>      
	

   <xsl:template match="cad_links">				
        <tr><td colspan="2"><center><b>Сведения об объектах (связь с кадастровыми номерами)</b></center></td></tr>
		<xsl:choose>
            <xsl:when test="parent_cad_number/cad_number"> 
		        <tr><td>Кадастровый номер объекта недвижимости (здания, сооружения), в пределах которого расположен объект недвижимости><xsl:value-of select = "parent_cad_number/cad_number"/></td></tr>
		    </xsl:when>		    
		    <xsl:when test="parent_cad_number/facility_cad_number"> 
		        <tr><td>Кадастровый номер предприятия как имущественного комплекса, в состав которого входит объект недвижимости: </td> <td><xsl:value-of select = "parent_cad_number/facility_cad_number"/></td></tr>
		    </xsl:when>
		    <xsl:when test="parent_cad_number/facility_cad_number">
		        <tr><td>Кадастровый номер единого недвижимого комплекса, в состав которого входит объект недвижимости: </td> <td><xsl:value-of select = "parent_cad_number/united_cad_number"/></td></tr>
		    </xsl:when>
		</xsl:choose>
   </xsl:template>	
   
   
   <xsl:template match="params">
        <tr><td colspan="2"><center><b>Характеристики объекта недвижимости</b></center></td></tr>
        <tr><td>Площадь, в кв. метрах: </td> <td><xsl:value-of select = "area"/></td></tr>    
   </xsl:template>
   
   
   <xsl:template match="address">
        <tr><td colspan="2"><center><b>Адрес</b></center></td></tr>
        <tr><td>Адрес в соответствии с ФИАС: </td> <td><xsl:value-of select = "readable_address"/></td></tr>    
   </xsl:template>
   
   <xsl:template match="location_in_build">
        <tr><td colspan="2"><center><b>Местоположение в объекте недвижимости (планы)</b></center></td></tr>
        <tr><td>Номер этажа: </td> <td><xsl:value-of select = "level/floor"/></td></tr>
        <tr><td>Тип этажа: </td> <td><xsl:value-of select = "level/floor_type/value"/></td></tr>    
        <xsl:choose>
            <xsl:when test="plans"> 
                <tr><td colspan="2"><center>
                    <xsl:for-each select="plans/plan"> 
                    </xsl:for-each>    
                </center></td></tr>
		    </xsl:when>		    
		</xsl:choose>
   </xsl:template>
   
   <xsl:template match="cost">
        <tr><td colspan="2"><center><b>Кадастровая стоимость</b></center></td></tr>
        <tr><td>Стоимость (руб.): </td> <td><xsl:value-of select = "value"/></td></tr>    
   </xsl:template>	
   	
</xsl:stylesheet>