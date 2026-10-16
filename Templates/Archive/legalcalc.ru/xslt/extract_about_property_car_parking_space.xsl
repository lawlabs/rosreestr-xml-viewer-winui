<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   
   <xsl:template match = "/"> 	
      <html> 
		
		<xsl:call-template name="style_tables"/>
		
        <body> 
            <center><h2>Выписка об объекте недвижимости (машиноместе)</h2></center> 
			<br/>				

			<xsl:apply-templates select="extract_about_property_car_parking_space/details_statement/group_top_requisites"/>
            
			<xsl:apply-templates select="extract_about_property_car_parking_space/car_parking_space_record"/>
			
			<xsl:apply-templates select="extract_about_property_car_parking_space/right_records"/>			
								
         </body> 
      </html> 
    </xsl:template>  
   
    <xsl:template match="group_top_requisites">				
        <center><h3>Реквизиты выписки</h3></center>
		<table title="«Реквизиты выписки»">
			<tr><td>Наименование органа регистрации прав: </td> <td><xsl:value-of select = "organ_registr_rights"/></td></tr>			
			<tr><td>Дата формирования выписки: </td><td><xsl:call-template name="formatDate"><xsl:with-param name="dateTime" select="date_formation"/></xsl:call-template></td></tr>
			<tr><td>Регистрационный номер: </td><td><xsl:value-of select = "registration_number"/></td></tr>
		</table>
    </xsl:template>   
   
    <xsl:template match="car_parking_space_record">				
		<br/><br/><br/>
        <center><h3>Сведения об объекте недвижимости – машиноместе</h3></center>
		<table>
			<tr><td>Дата постановки на учет/ регистрации: </td> <td><xsl:call-template name="formatDateTime"><xsl:with-param name="dateTime" select="record_info/registration_date"/></xsl:call-template></td></tr>			
			<tr><td>Дата снятия с учета/регистрации: </td> <td><xsl:call-template name="formatDateTime"><xsl:with-param name="dateTime" select="record_info/cancel_date"/></xsl:call-template></td></tr>						
			<tr><td colspan="2"><center><b>Общие сведения.</b></center></td></tr>
			<xsl:apply-templates select="object/common_data"/>
			<xsl:apply-templates select="cad_links"/>
			<xsl:apply-templates select="params"/>
			<xsl:apply-templates select="address_room/address/address"/>
			<xsl:apply-templates select="location_in_build"/>
            <xsl:apply-templates select="cost"/>			
		</table>
    </xsl:template>   
	
	<xsl:include href="https://legalcalc.ru/xslt/right_records.xsl"/>
	         
	<xsl:template match="common_data">				
			<tr><td>Кадастровый номер: </td> <td><xsl:value-of select = "cad_number"/></td></tr>			
			<tr><td>Номер кадастрового квартала: </td> <td><xsl:value-of select = "quarter_cad_number"/></td></tr>									
    </xsl:template>      
	

   <xsl:template match="cad_links">				
        <tr><td colspan="2"><center><b>Сведения об объектах (связь с кадастровыми номерами)</b></center></td></tr>
		<xsl:choose>
            <xsl:when test="parent_cad_number/cad_number"> 
		        <tr><td>Кадастровый номер объекта недвижимости (здания, сооружения), в пределах которого расположено машиноместо: </td> <td><xsl:value-of select = "parent_cad_number/cad_number"/></td></tr>
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
        <tr><td colspan="2"><center><b>Характеристики машиноместа</b></center></td></tr>
        <tr><td>Площадь, в кв. метрах: </td> <td><xsl:value-of select = "area"/></td></tr>    
   </xsl:template>
   
   
   <xsl:template match="address">
        <tr><td colspan="2"><center><b>Адрес машиноместа</b></center></td></tr>
        <tr><td>Адрес в соответствии с ФИАС: </td> <td><xsl:value-of select = "readable_address"/></td></tr>    
   </xsl:template>
   
   <xsl:template match="location_in_build">
        <tr><td colspan="2"><center><b>Местоположение машиноместа в объекте недвижимости (планы)</b></center></td></tr>
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
   
   <xsl:include href="https://legalcalc.ru/xslt/styles/style_tables.xsl"/>	   
   
   
</xsl:stylesheet>