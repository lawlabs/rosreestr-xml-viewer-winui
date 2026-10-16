<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   

	<xsl:template match="common_data">				
			<tr><td>Кадастровый номер: </td> <td><xsl:value-of select = "cad_number"/></td></tr>			
			<tr><td>Номер кадастрового квартала: </td> <td><xsl:value-of select = "quarter_cad_number"/></td></tr>									
    </xsl:template>      
	

   <xsl:template match="cad_links">				
        <tr><td colspan="2"  class="row_group_1"><center>Сведения об объектах (связь с кадастровыми номерами)</center></td></tr>
		<xsl:choose>
            <xsl:when test="parent_cad_number/cad_number"> 
		        <tr><td>Кадастровый номер объекта недвижимости (здания, сооружения), в пределах которого расположен объект недвижимости</td>
				<td><xsl:value-of select = "parent_cad_number/cad_number"/></td></tr>
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
        <tr><td colspan="2" class="row_group_1"><center>Характеристики помещения</center></td></tr>
        <tr><td>Площадь, в кв. метрах: </td> <td><xsl:value-of select = "area"/></td></tr>    
		<tr><td>Наименование: </td> <td><xsl:value-of select = "name"/></td></tr>    
		<tr><td>Назначение: </td> <td><xsl:value-of select = "purpose/value"/></td></tr>    
		<tr><td>Вид: </td> <td><xsl:value-of select = "type/value"/></td></tr>    
		<tr>
			<td>Общее имущество собственников помещений в многоквартирном доме: </td>
			<td>
				<xsl:choose>
					<xsl:when test="common_property=true"> 
						да
					</xsl:when>		    
					<xsl:otherwise>
						нет
					</xsl:otherwise>
				</xsl:choose>
			</td>
		</tr>    
		<tr>
			<td>Имущество общего пользования: </td>
			<td>
				<xsl:choose>
					<xsl:when test="service=true"> 
						да
					</xsl:when>		    
					<xsl:otherwise>
						нет
					</xsl:otherwise>
				</xsl:choose>
			</td>
		</tr>
   </xsl:template>
   
   
   <xsl:template match="address">
        <tr><td colspan="2"  class="row_group_1"><center>Адрес</center></td></tr>
        <tr><td>Адрес в соответствии с ФИАС: </td> <td><xsl:value-of select = "readable_address"/></td></tr>    
   </xsl:template>
   
   <xsl:template match="location_in_build">
        <tr><td colspan="2"  class="row_group_1"><center>Местоположение в объекте недвижимости (планы)</center></td></tr>
        <tr><td>Номер этажа: </td> <td><xsl:value-of select = "level/floor"/></td></tr>
        <tr><td>Тип этажа: </td> <td><xsl:value-of select = "level/floor_type/value"/></td></tr>    
        <xsl:choose>
            <xsl:when test="plans"> 
                <tr><td colspan="2" class="row_group_1"><center>
                    <xsl:for-each select="plans/plan"> 
                    </xsl:for-each>    
                </center></td></tr>
		    </xsl:when>		    
		</xsl:choose>
   </xsl:template>
   
   <xsl:template match="cost">
        <tr><td colspan="2"  class="row_group_1"><center>Кадастровая стоимость</center></td></tr>
        <tr><td>Стоимость (руб.): </td> <td><xsl:value-of select = "value"/></td></tr>    
   </xsl:template>	
   	
</xsl:stylesheet>